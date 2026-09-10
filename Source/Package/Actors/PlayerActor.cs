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
using Orleans;
using Orleans.Runtime;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Utils;
using SyncnetPlatform.Utils.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncnetPlatform.Actors;

public enum PlayRoomMemberUpdateReason
{
    None = 0,
    Join = 1,
    Leave = 2,
    Vanished = 3,
}

[GenerateSerializer]
public class PlayerState
{
    [Id(0)] public int Id { get; set; }
    [Id(1)] public Guid PlayerId { get; set; }
    [Id(2)] public string PlayerName { get; set; } = String.Empty;

    [Id(3)] public Dictionary<string, object?> Extension { get; set; } = new();

    public object? this[string key]
    {
        get => Extension.TryGetValue(key, out var val) ? val : null;
        set => Extension[key] = value;
    }
}

[GenerateSerializer] 
public class PlayRoomMember(Guid roomId, Guid playerId, string playerName, byte[]? playerExtendData)
{
    [Id(0)]
    public Guid RoomId { get; set; } = roomId;

    [Id(1)]
    public Guid PlayerId { get; set; } = playerId;

    [Id(2)]
    public string PlayerName { get; set; } = playerName;

    // One time use only.
    [Id(3)]
    public byte[]? PlayerExtendData { get; set; } = playerExtendData;
}

public partial class PlayerActor : Grain, IPlayerActor, IPacketHandlerActor, IPacketHandler
{
    private readonly struct PendingPacket(byte[] data, Activity? queueActivity)
    {
        public byte[] Data { get; } = data;
        public Activity? QueueActivity { get; } = queueActivity;
    }

    private readonly ILogger<PlayerActor> _logger;
    private Guid PlayerId => GrainContext.GrainId.GetGuidKey();
    private readonly IPlayerModelRepository _playerModelRepository;

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
    protected List<Guid> _joinedRoomList = new();

    // Custom behavior supporting
    private readonly IPlayerCustomBehavior? _playerCustomBehavior;

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepository playerModelRepository,
        IPacketRouter routeTable,
        IPlayerCustomBehavior? playerCustomBehavior = null
        )
    {
        _logger = logger;
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

    public async ValueTask SetOnline(bool isOnline)
    {
        if(isOnline == true )
        {
            _playerState = await _playerModelRepository.GetOrCreate(PlayerId);
            Dbid = _playerState.Id;
            _IsOnline = true;

            if (_playerCustomBehavior != null)
            {
                var needToUpdateDb = await _playerCustomBehavior.OnLoginAsync(_playerState);
                if (needToUpdateDb)
                {
                    await _playerModelRepository.Update(_playerState);
                }
            }
        }
        else
        {
            _IsOnline = false;
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


    public Task UpdatePlayerName(string newName)
    {
        _playerState.PlayerName = newName;
        _IsDirtyPlayerData = true;
        return Task.CompletedTask;
    }

    public Task<string> GetPlayerName()
    {
        return Task.FromResult(_playerState.PlayerName); 
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

    public async Task<(PacketErrorCodes ,Guid, byte[]?)> CreateAndJoinPlayRoom(
        string roomName,
        bool isPrivate,
        int maxCapacity,
        string roomPassword,
        byte[] playerMetadata)
    {
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        byte[]? serializedPlayRoomState = null;
        Guid newPlayRoomId = Guid.NewGuid();
        
        (errorCode, serializedPlayRoomState) = await CreatePlayRoom(newPlayRoomId, roomName, isPrivate, maxCapacity, roomPassword);
        if (errorCode != PacketErrorCodes.Success) return (errorCode, newPlayRoomId, serializedPlayRoomState);

        (errorCode, serializedPlayRoomState) = await JoinRoom(newPlayRoomId);
        if (errorCode != PacketErrorCodes.Success) return (errorCode, newPlayRoomId, serializedPlayRoomState);
        
        // Just remember rooms I joined.
        _joinedRoomList.Add(newPlayRoomId);

        // Delegating additional process to user's handler.
        _playerCustomBehavior?.OnJoinPlayRoom(_playerState, newPlayRoomId, isOwner: true, serializedPlayRoomState);
        
        return (errorCode, newPlayRoomId, serializedPlayRoomState);
    }

    protected async Task<(PacketErrorCodes, byte[]?)> CreatePlayRoom(Guid newPlayRoomId, string roomName, bool isPrivate, int maxCapacity, string roomPassword)
    {
        IPlayRoomActor newPlayRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(newPlayRoomId);

        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        byte[]? serializedPlayRoomState = null;

        (errorCode, serializedPlayRoomState) = await newPlayRoomActor.SetRoomInformation(
            roomName, 
            isPrivate, 
            maxCapacity, 
            roomPassword, 
            BuildPlayerRoomMember(newPlayRoomId));
        
        
        return (errorCode, serializedPlayRoomState);
    }

    protected async Task<(PacketErrorCodes, byte[])> JoinRoom(Guid playRoomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        (errorCode, byte[] playRoomCustomState) = await playRoomActor.JoinPlayer(BuildPlayerRoomMember(playRoomId));
        
        if (errorCode == PacketErrorCodes.Success) _joinedRoomList.Add(playRoomId);

        return (errorCode, playRoomCustomState);
    }

    public async Task<(PacketErrorCodes, byte[])> JoinPlayRoom(Guid roomId)
    {
        if(!_IsOnline)
        {
            return (PacketErrorCodes.PlayerOffline, Array.Empty<byte>());
        }

        PacketErrorCodes errorCode = PacketErrorCodes.Success;

        (errorCode, byte[] playRoomCustomState) = await JoinRoom(roomId);
        
        return (errorCode, playRoomCustomState);
    }
    protected PlayRoomMember BuildPlayerRoomMember(Guid roomId) 
        => new PlayRoomMember(roomId, GrainContext.GrainId.GetGuidKey(), _playerState.PlayerName, SerializePlayerExtendData());

    /// <summary>
    /// Be called when members of a room has changed. - in and out.
    /// </summary>
    /// <param name="playRoomMember"></param>
    /// <param name="updateReason"></param>
    /// <returns></returns>
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

    public async Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        List<PlayRoomMember> players = await playRoomActor.GetPlayersInPlayRoom();
        return players;
    }
    
    public async Task PlayerActionToPlayRoom(Guid roomId, string actionType, byte[] actionParameter)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        await playRoomActor.OnPlayerActionToPlayRoom(PlayerId, actionType, actionParameter);
    }

    public async Task<PacketErrorCodes> LeavePlayRoom(Guid roomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        PacketErrorCodes result = await playRoomActor.LeavePlayer(BuildPlayerRoomMember(roomId));
        _joinedRoomList.Remove(roomId);

        return result;
    }

    public async Task Broadcast(Guid playRoomId, string message)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);

    }
}


