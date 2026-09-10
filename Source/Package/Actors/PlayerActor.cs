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

public partial class PlayerActor : Grain, IPlayerActor, IPacketHandlerActor, IPacketHandler
{
    private readonly struct PendingPacket(byte[] data, Activity? queueActivity)
    {
        public byte[] Data { get; } = data;
        public Activity? QueueActivity { get; } = queueActivity;
    }

    private readonly ILogger<PlayerActor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Guid PlayerId => GrainContext.GrainId.GetGuidKey();
    private readonly IPlayerModelRepository _playerModelRepository;

    // Components
    private IPlayRoomComponent? _playRoomComponent = null;

    // Session Service
    private readonly IPacketRouter _routeTable;
    private readonly Channel<PendingPacket> _receiveQueueChannel;
    private CancellationTokenSource? _ctsForRunRoutingPackets;
    private Task? _runRoutingPackets;
    private ISendDataGrain _sendDataGrain = null!;
    
    
    private bool _isPlayerStatsDelegated = false;

    // player data

    /// <summary>
    /// Primary key for the player data table
    /// </summary>
    protected int Dbid;
    protected string _name = String.Empty;
    

    /// <summary>
    /// the platform authenticated from.
    /// </summary>
    protected SupportedPlatformType _idpFrom;
    

    protected PlayerState _playerState = new();

    /// <summary>
    /// This indicates the actor has been activated from real player with corrent websocket connection.
    /// </summary>
    protected bool _IsOnline = false;

    protected bool _IsDirtyPlayerData = false;

    /// <summary>
    /// Player can join multiple rooms at the same time.
    /// </summary>
    // protected List<Guid> _joinedRoomList = new();

    // Custom behavior supporting
    private readonly IPlayerCustomBehavior? _playerCustomBehavior;

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepository playerModelRepository,
        IPacketRouter routeTable,
        IServiceProvider serviceProvider,
        IPlayerCustomBehavior? playerCustomBehavior = null
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _playerModelRepository = playerModelRepository;
        _routeTable = routeTable;

        _receiveQueueChannel = Channel.CreateBounded<PendingPacket>(new BoundedChannelOptions(150)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
        
        _playerCustomBehavior = playerCustomBehavior;
    }

    protected void InitializeComponents()
    {
        _playRoomComponent = ActivatorUtilities.CreateInstance<IPlayRoomComponent>(_serviceProvider, 
            PlayerId,
            _playerState, 
            _playerCustomBehavior!);
    }

    protected void DeinitializeComponents()
    {
        _playRoomComponent = null;
    }

    public async ValueTask SetOnline(bool isOnline)
    {
        if(isOnline == true )
        {
            _playerState = await _playerModelRepository.GetOrCreate(PlayerId);
            Dbid = _playerState.Id;
            _IsOnline = true;

            if (_playerCustomBehavior != null)
            {
                bool needToUpdateDb = await _playerCustomBehavior.OnLoginAsync(_playerState);
                if (needToUpdateDb)
                {
                    await _playerModelRepository.Update(_playerState);
                }
            }
            InitializeComponents();
            
            
        }
        else
        {
            _IsOnline = false;
            DeinitializeComponents();
            this.DelayDeactivation(TimeSpan.FromMinutes(1));
        }
    }
    public ValueTask SetIdProvider(SupportedPlatformType idpFrom) 
    {
        _idpFrom = idpFrom;
        return ValueTask.CompletedTask;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _ctsForRunRoutingPackets = new CancellationTokenSource();
        _sendDataGrain = GrainFactory.GetGrain<ISendDataGrain>(this.GetGrainId().GetGuidKey());

        _runRoutingPackets = RunRoutingPackets(_ctsForRunRoutingPackets.Token);

        _routeTable.BuildParamExtractionFuncs<PacketWrapper>();
        _routeTable.BuildPacketHandlerFunctions<PlayerActor>(this);
        
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        bool needToUpdateDb = false;
        if (_playerCustomBehavior != null)
        {
            needToUpdateDb = await _playerCustomBehavior.OnLogoutAsync(cancellationToken);
        }

        if (_ctsForRunRoutingPackets is not null) await _ctsForRunRoutingPackets.CancelAsync();

        _receiveQueueChannel.Writer.TryComplete();
        if (_runRoutingPackets != null) await _runRoutingPackets;

        if(_IsDirtyPlayerData || needToUpdateDb)
        {
            await _playerModelRepository.Update(_playerState);
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    protected byte[] SerializePlayerExtendData()
    {
        if(_playerCustomBehavior is not null)
        {
            return _playerCustomBehavior.GetPlayerCustomState().Serialize(_playerState.Extension);
        }
        return [];
    }

    protected Dictionary<string, object?> DeserializePlayerExtendData(byte[] data)
    {
        if(_playerCustomBehavior is not null)
        {
            return _playerCustomBehavior.GetPlayerCustomState().Deserialize(data);
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
        if (!_IsOnline || _sendDataGrain == null)
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
        if (!_IsOnline || _sendDataGrain == null) return;

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
        if( !_IsOnline || _sendDataGrain == null) return; 
        
        if(_playerCustomBehavior is not null)
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
        if (_IsOnline && _sendDataGrain != null)
        {
            await _sendDataGrain.Send(PacketBuilder.Build(new OnPlayRoomStateUpdateArgs(roomId, customState)));
        }
    }

    public ValueTask<bool> IsDelegatingPlayerStats()
    {
        return ValueTask.FromResult(_isPlayerStatsDelegated);
    }

    private void SetPlayerStatsDelegated(bool isDelegatingNow)
    {
        _isPlayerStatsDelegated = isDelegatingNow;
    }

    
    
    public async Task PlayerActionToPlayRoom(Guid roomId, string actionType, byte[] actionParameter)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        await playRoomActor.OnPlayerActionToPlayRoom(PlayerId, actionType, actionParameter);
    }
}


