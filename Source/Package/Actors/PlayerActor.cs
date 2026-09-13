using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Orleans.Concurrency;
using SyncnetPlatform.Controllers;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Repositories;
using System.ComponentModel.DataAnnotations;
using PacketBuilder = SyncnetPlatform.Network.Utils.SyncnetPacketBuilder;
using System.Threading.Channels;
using System.Diagnostics;
using Google.FlatBuffers;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using SyncnetPlatform.Actors.Components;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Utils;
using SyncnetPlatform.Utils.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncnetPlatform.Actors;

public partial class PlayerActor(
    ILogger<PlayerActor> logger,
    IPlayerModelRepository playerModelRepository,
    IPacketRouter routeTable,
    IServiceProvider serviceProvider,
    IPlayerCustomBehavior? playerCustomBehavior = null)
    : Grain, IPlayerActor, IPacketHandlerActor, IPacketHandler
{

    private Guid PlayerId => GrainContext.GrainId.GetGuidKey();

    // Components
    private IPlayRoomSession? _playRoomSession = null;

    // Session Service
    private readonly Channel<PendingPacket> _receiveQueueChannel = Channel.CreateBounded<PendingPacket>(new BoundedChannelOptions(150)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = false
    });
    private CancellationTokenSource? _ctsForRunRoutingPackets;
    private Task? _runRoutingPackets;
    private ISendDataGrain? _sendDataGrain = null!;
    
    
    private bool _isPlayerStatsDelegated = false;


    // Player's properties.
    private int _dbid;
    private string _name = string.Empty;
    private PlayerState _playerState = new();
    private bool _isOnline = false;
    private bool _isDirtyPlayerData = false;


    private void InitializeComponents()
    {
        _playRoomSession = ActivatorUtilities.CreateInstance<PlayRoomSession>(serviceProvider, PlayerId,_playerState);
        if(playerCustomBehavior is not null) _playRoomSession.SetPlayerCustomBehavior(playerCustomBehavior);
        
    }

    private void DeinitializeComponents()
    {
        _playRoomSession = null;
    }

    public async ValueTask SetOnline(bool isOnline)
    {
        if(isOnline == true )
        {
            _playerState = await playerModelRepository.GetOrCreate(PlayerId);
            _dbid = _playerState.Id;
            _isOnline = true;

            if (playerCustomBehavior != null)
            {
                bool needToUpdateDb = await playerCustomBehavior.OnLoginAsync(_playerState);
                if (needToUpdateDb)
                {
                    await playerModelRepository.Update(_playerState);
                }
            }
            InitializeComponents();
            
            
        }
        else
        {
            _isOnline = false;
            DeinitializeComponents();
            this.DelayDeactivation(TimeSpan.FromMinutes(1));
        }
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        SetupNetworkProcessingUnits();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        bool needToUpdateDb = false;
        if (playerCustomBehavior != null)
        {
            needToUpdateDb = await playerCustomBehavior.OnLogoutAsync(cancellationToken);
        }

        if (_ctsForRunRoutingPackets is not null) await _ctsForRunRoutingPackets.CancelAsync();

        _receiveQueueChannel.Writer.TryComplete();
        if (_runRoutingPackets != null) await _runRoutingPackets;

        if(_isDirtyPlayerData || needToUpdateDb)
        {
            await playerModelRepository.Update(_playerState);
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    private Dictionary<string, object?> DeserializePlayerExtendData(byte[] data)
    {
        if(playerCustomBehavior is not null)
        {
            return playerCustomBehavior.GetPlayerCustomState().Deserialize(data);
        }
        return new Dictionary<string, object?>(capacity: 0);
    }

    public async Task<PacketErrorCodes> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType)
    {
        IPlayerActor targetPlayer = GrainFactory.GetGrain<IPlayerActor>(toPlayerId);
        return await targetPlayer.OnDirectDeliveryData(GrainContext.GrainId.GetGuidKey(), message, dataType);
    }
    
    public async Task<PacketErrorCodes> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType)
    {
        if (!_isOnline || _sendDataGrain == null)
        {
            return PacketErrorCodes.PlayerOffline;
        }
        OnDirectDeliveryDataArgs data = new OnDirectDeliveryDataArgs(fromPlayerId, message, dataType);
        await _sendDataGrain.Send(PacketBuilder.Build<OnDirectDeliveryDataArgs>(data));
        return PacketErrorCodes.Success;
    }

    [OneWay] 
    public async ValueTask OnUpdateForPlayRoomMembers(PlayRoomMember playRoomMember,
        PlayRoomMemberUpdateReason updateReason)
    {
        if (!_isOnline || _sendDataGrain == null) return;

        switch (updateReason)
        {
            case PlayRoomMemberUpdateReason.Join:
                await _sendDataGrain.Send(PacketBuilder.Build<OnPlayerJoinRoomArgs>(
                    new OnPlayerJoinRoomArgs(
                        playRoomMember.RoomId,
                        playRoomMember.PlayerId,
                        playRoomMember.PlayerName,
                        playRoomMember.PlayerExtendData
                    )
                    ));
                break;
            case PlayRoomMemberUpdateReason.Leave:
                await _sendDataGrain.Send(PacketBuilder.Build<OnPlayerLeaveRoomArgs>(
                    new OnPlayerLeaveRoomArgs(
                        playRoomMember.RoomId,
                        playRoomMember.PlayerId,
                        playRoomMember.PlayerName
                    )
                    ));
                break;
        }
    }
    [OneWay]
    public async ValueTask OnUpdatePlayerExtendData(byte[] extendData)
    {
        if( !_isOnline || _sendDataGrain == null) return; 
        
        if(playerCustomBehavior is not null)
        {
            _playerState.Extension = DeserializePlayerExtendData(extendData);
            await _sendDataGrain.Send
            (
                PacketBuilder.Build
                (
                    new OnPlayRoomUpdatePlayerExtendDataArgs(PlayerId, extendData)
                )
            );
        }
    }

    [OneWay]
    public async ValueTask OnUpdatePlayRoomCustomState(Guid roomId, byte[] customState)
    {
        if (!_isOnline || _sendDataGrain == null) return;
        await _sendDataGrain.Send(PacketBuilder.Build(new OnPlayRoomStateUpdateArgs(roomId, customState)));
    }

    [OneWay]
    public async ValueTask OnPlayerActionToPlayRoomResult(Guid roomId, string resultType, byte[] resultParameters)
    {
        if (!_isOnline || _sendDataGrain == null) return;
        await _sendDataGrain.Send(PacketBuilder.Build(new OnPlayerActionToPlayRoomResultArgs(resultType, resultParameters)));
    }

    public ValueTask<bool> IsDelegatingPlayerStats()
    {
        return ValueTask.FromResult(_isPlayerStatsDelegated);
    }

    private void SetPlayerStatsDelegated(bool isDelegatingNow)
    {
        _isPlayerStatsDelegated = isDelegatingNow;
    }
    
    private readonly struct PendingPacket(byte[] data, Activity? queueActivity)
    {
        public byte[] Data { get; } = data;
        public Activity? QueueActivity { get; } = queueActivity;
    }
}


