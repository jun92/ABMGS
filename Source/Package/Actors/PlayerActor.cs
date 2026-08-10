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
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Utils;
using SyncnetPlatform.Utils.Telemetry;
 
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

[GenerateSerializer] //public record PlayRoomMember(Guid RoomId, Guid PlayerId, string PlayerName, byte[]? PlayerExtendData);
public class PlayRoomMember
{
    public PlayRoomMember(Guid roomId, Guid playerId, string playerName, byte[]? playerExtendData)
    {
        RoomId = roomId;
        PlayerId = playerId;
        PlayerName = playerName;
        PlayerExtendData = playerExtendData;
    }

    [Id(0)]
    public Guid RoomId { get; set; }
    [Id(1)]
    public Guid PlayerId { get; set; }
    [Id(2)]
    public string PlayerName { get; set; }
    [Id(3)]
    public byte[]? PlayerExtendData { get; set; }


}

public partial class PlayerActor : Grain, IPlayerActor, IPacketHandlerActor, IPacketHandler
{
    private readonly struct PendingPacket
    {
        public byte[] Data { get; }
        public Activity? QueueActivity { get; }

        public PendingPacket(byte[] data, Activity? queueActivity)
        {
            Data = data;
            QueueActivity = queueActivity;
        }
    }

    private readonly ILogger<PlayerActor> _logger;
    private readonly IPlayerModelRepository _playerModelRepository;

    private readonly IPacketRouter _routeTable;
    private readonly Channel<PendingPacket> _receiveQueueChannel;
    private CancellationTokenSource? _ctsForRunRoutingPackets;
    private Task? _runRoutingPackets;
    private ISendDataGrain _sendDataGrain = null!;

    // player data

    /// <summary>
    /// Primary key for the player data table
    /// </summary>
    protected int _dbid;
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

    public async Task SetOnline(bool isOnline)
    {
        if(isOnline == true )
        {
            Guid thisPlayerId = GrainContext.GrainId.GetGuidKey();
            _playerState = await _playerModelRepository.GetOrCreate(thisPlayerId);
            _dbid = _playerState.Id;
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
    public async Task SetIdProvider(SupportedPlatformType idpFrom)
    {
        _idpFrom = idpFrom;
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
            needToUpdateDb = await _playerCustomBehavior.OnLogoutAsync(_playerState, cancellationToken);
        }

        _ctsForRunRoutingPackets?.Cancel();
        _receiveQueueChannel.Writer.TryComplete();
        if (_runRoutingPackets != null) await _runRoutingPackets;

        if(_IsDirtyPlayerData || needToUpdateDb)
        {
            await _playerModelRepository.Update(_playerState);
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task Echo(int seq)
    {
        return Task.CompletedTask;
    }

    public async Task PingPong(int seq)
    {
        if(!_IsOnline)
        {
            return;
        }
        await _sendDataGrain.Send(PacketBuilder.Build<PongArgs>(new PongArgs(seq + 1)));
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
            return _playerCustomBehavior.SerializePlayerExtendData(_playerState.Extension);
        }
        else
        {
            return Array.Empty<byte>();
        }
    }

    protected Dictionary<string, object?> DeserializePlayerExtendData(byte[] data)
    {
        if(_playerCustomBehavior is not null)
        {
            return _playerCustomBehavior.DeserializePlayerExtendData(data);
        }
        else
        {
            return new Dictionary<string, object?>(capacity: 0);
        }
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

    public async Task<(Guid, IPlayRoomCustomState?)> CreateAndJoinPlayRoom(
        string roomName, 
        bool isPrivate, 
        int maxCapacity, 
        string roomPassword,
        byte[] playerMetadata)
    {
        Guid newPlayRoomId = Guid.NewGuid();
        
        // Grab a new PlayRoomActor.
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(newPlayRoomId);

        // Supply initial data to play room.
        IPlayRoomCustomState? playRoomMetaData = await playRoomActor.SetRoomInformation(roomName, isPrivate, maxCapacity, roomPassword, BuildPlayerRoomMember(newPlayRoomId));
        
        // Just remember rooms I joined.
        _joinedRoomList.Add(newPlayRoomId);

        // Delegating additional process to user's handler.
        _playerCustomBehavior?.OnJoinPlayRoom(_playerState, newPlayRoomId, isOwner: true, playRoomMetaData);
        
        return (newPlayRoomId, playRoomMetaData);
    }

    public async Task<(PacketErrorCodes, byte[])> JoinPlayRoom(Guid roomId)
    {
        if(!_IsOnline)
        {
            return (PacketErrorCodes.PlayerOffline, Array.Empty<byte>());
        }
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        if(!await playRoomActor.IsValidRoomToJoin())
        {
            return (PacketErrorCodes.RoomNotFound, Array.Empty<byte>());
        }
        var(result, playRoomCustomState) = await playRoomActor.JoinPlayer(BuildPlayerRoomMember(roomId));
        if(result == PacketErrorCodes.Success)
        {
            _joinedRoomList.Add(roomId);
        }
        return (result, playRoomCustomState);
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
    public async Task OnUpdateForPlayRoomMembers(PlayRoomMember playRoomMember, PlayRoomMemberUpdateReason updateReason )
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
    public async Task OnUpdatePlayerExtendData(byte[] extendData)
    {
        if(_playerCustomBehavior is not null)
        {
            _playerState.Extension = DeserializePlayerExtendData(extendData);
        }
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

    public Guid PlayerId => GrainContext.GrainId.GetGuidKey();


    

    public async Task OnHandleCustomPacket(byte[] customPacket)
    {
        if(_playerCustomBehavior is not null)
        {
            await _playerCustomBehavior.HandleCustomPacket(customPacket);
        }
    }
}


