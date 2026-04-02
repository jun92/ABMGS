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

namespace SyncnetPlatform.Actors;

public interface IPlayerBehavior
{

}

public enum PlayRoomMemberUpdateReason
{
    None = 0,
    Join = 1,
    Leave = 2,
    Vanished = 3,
}

[GenerateSerializer] public record PlayRoomMember(Guid RoomId, Guid PlayerId, string PlayerName);

public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;
    private readonly IPlayerModelRepository _playerModelRepository;

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
    

    protected PlayerData _playerData = new();
    protected IPacketHandlerActor? _packetHandler = null;

    /// <summary>
    /// This indicates the actor has been activated from real player with corrent websocket connection.
    /// </summary>
    protected bool _IsOnline = false;

    protected bool _IsDirtyPlayerData = false;

    /// <summary>
    /// Player can join multiple rooms at the same time.
    /// </summary>
    protected List<Guid> _joinedRoomList = new();

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepository playerModelRepository
        )
    {
        _logger = logger;
        _playerModelRepository = playerModelRepository;
        
    }

    public async Task SetOnline(bool isOnline)
    {
        _IsOnline = isOnline;
        if(isOnline == true )
        {
            Guid ThisPlayerId = GrainContext.GrainId.GetGuidKey();
            _playerData = await _playerModelRepository.GetOrCreate(ThisPlayerId);
            _dbid = _playerData.Id;
            _packetHandler = GrainFactory.GetGrain<IPacketHandlerActor>(ThisPlayerId);
        }
        else
        {
            _packetHandler = null;
            this.DelayDeactivation(TimeSpan.FromMinutes(1));
        }
    }
    public async Task SetIdProvider(SupportedPlatformType idpFrom)
    {
        _idpFrom = idpFrom;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if(_IsDirtyPlayerData)
        {
            await _playerModelRepository.Update(_playerData);
        }
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task Echo(int seq)
    {
        return Task.CompletedTask;
    }

    public Task PingPong(int seq)
    {
        if(!_IsOnline)
        {
            return Task.CompletedTask;
        }
        _packetHandler!.PushSendData<PongArgs>(new PongArgs(seq + 1));
        return Task.CompletedTask;
    }

    public Task UpdatePlayerName(string newName)
    {
        _playerData.PlayerName = newName;
        _IsDirtyPlayerData = true;
        return Task.CompletedTask;
    }

    public Task<string> GetPlayerName()
    {
        return Task.FromResult(_playerData.PlayerName); 
    }

    public async Task<PacketErrorCodes> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType)
    {
        IPlayerActor targetPlayer = GrainFactory.GetGrain<IPlayerActor>(toPlayerId);
        return await targetPlayer.OnDirectDeliveryData(GrainContext.GrainId.GetGuidKey(), message, dataType);
    }
    public async Task<PacketErrorCodes> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType)
    {
        if(!_IsOnline)
        {
            return PacketErrorCodes.PlayerOffline;
        }
        OnDirectDeliveryDataArgs data = new OnDirectDeliveryDataArgs(fromPlayerId, message, dataType);
        await _packetHandler!.PushSendData<OnDirectDeliveryDataArgs>(data);
        return PacketErrorCodes.Success;
    }

    public async Task<Guid> CreateAndJoinPlayRoom(string roomName, bool isPrivate, int maxCapacity, string roomPassword)
    {
        Guid newPlayRoomId = Guid.NewGuid();
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(newPlayRoomId);

        await playRoomActor.SetRoomInformation(roomName, isPrivate, maxCapacity, roomPassword, BuildPlayerRoomMember(newPlayRoomId));
        _joinedRoomList.Add(newPlayRoomId);
        return newPlayRoomId;
    }

    public async Task<PacketErrorCodes> JoinPlayRoom(Guid roomId)
    {
        if(!_IsOnline)
        {
            return PacketErrorCodes.PlayerOffline;
        }
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        if(!await playRoomActor.IsValidRoomToJoin())
        {
            return PacketErrorCodes.RoomNotFound;
        }
        await playRoomActor.JoinPlayer(BuildPlayerRoomMember(roomId));
        _joinedRoomList.Add(roomId);
        return PacketErrorCodes.Success;
    }
    protected PlayRoomMember BuildPlayerRoomMember(Guid roomId) => new PlayRoomMember(roomId, GrainContext.GrainId.GetGuidKey(), _playerData.PlayerName);

    /// <summary>
    /// Be called when members of a room has changed. - in and out.
    /// </summary>
    /// <param name="playRoomMember"></param>
    /// <param name="updateReason"></param>
    /// <returns></returns>
    [OneWay]
    public async Task OnUpdateForPlayRoomMembers(PlayRoomMember playRoomMember, PlayRoomMemberUpdateReason updateReason )
    {
        if (!_IsOnline) return;
        switch (updateReason)
        {
            case PlayRoomMemberUpdateReason.Join:
                await _packetHandler!.PushSendData<OnPlayerJoinRoomArgs>(
                    new OnPlayerJoinRoomArgs(
                        playRoomMember.RoomId,
                        playRoomMember.PlayerId,
                        playRoomMember.PlayerName
                    )
                    );
                break;
            case PlayRoomMemberUpdateReason.Leave:
                await _packetHandler!.PushSendData<OnPlayerLeaveRoomArgs>(
                    new OnPlayerLeaveRoomArgs
                    (
                        playRoomMember.RoomId,
                        playRoomMember.PlayerId,
                        playRoomMember.PlayerName
                    )
                    );
                break;
        }
    }

    public async Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        List<PlayRoomMember> Players = await playRoomActor.GetPlayersInPlayRoom();
        return Players;
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


