using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SyncnetPlatform.Controllers;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Actors;
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
    /// <summary>
    /// Represents the packet sender used to transmit data packets.
    /// </summary>
    /// <remarks>This field is protected and intended for use by derived classes. Assign a valid
    /// implementation of ISendDataGrain before attempting to send packets.</remarks>
    protected ISendDataGrain? _packetSender = null;

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
            _packetSender = GrainFactory.GetGrain<ISendDataGrain>(ThisPlayerId);
        }
        else
        {
            _packetSender = null;
            this.DelayDeactivation(TimeSpan.FromMinutes(1));
        }
    }
    public async Task SetIdProvider(SupportedPlatformType idpFrom)
    {
        _idpFrom = idpFrom;
    }

    //public override async Task OnActivateAsync(CancellationToken cancellationToken)
    //{
    //    await base.OnActivateAsync(cancellationToken);
    //}

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

    public Task UpdatePlayerName(string newName)
    {
        _playerData.PlayerName = newName;
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
        
        await _packetSender!.Send(
            PacketBuilder.Build(
                new OnDirectDeliveryDataArgs(fromPlayerId, message, dataType)));
        return PacketErrorCodes.Success;
    }

    public async Task<Guid> CreateAndJoinPlayRoom(string roomName, bool isPrivate, int maxCapacity, string roomPassword)
    {
        Guid newPlayRoomId = Guid.NewGuid();
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(newPlayRoomId);

        await playRoomActor.SetRoomInformation(roomName, isPrivate, maxCapacity, roomPassword, GrainContext.GrainId.GetGuidKey());
        _joinedRoomList.Add(newPlayRoomId);
        return newPlayRoomId;
    }

    public async Task<PacketErrorCodes> JoinPlayRoom(Guid playRoomId)
    {
        if(!_IsOnline)
        {
            return PacketErrorCodes.PlayerOffline;
        }
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);
        if(!await playRoomActor.IsValidRoomToJoin())
        {
            return PacketErrorCodes.RoomNotFound;
        }
        await playRoomActor.OnPlayerJoin(GrainContext.GrainId.GetGuidKey());
        _joinedRoomList.Add(playRoomId);
        return PacketErrorCodes.Success;
    }

    public async Task<bool> OnPlayerJoinRoom(Guid roomId, Guid playerId, string playerName)
    {
        if (!_IsOnline)
        {
            return false;
        }

        await _packetSender!.Send(
            PacketBuilder.Build<OnPlayerJoinRoomArgs>(
                new OnPlayerJoinRoomArgs(roomId, playerId, playerName)));
        return true;
    }

    public async Task LeavePlayRoom(Guid playRoomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);
        _joinedRoomList.Remove(playRoomId);

    }

    public async Task DestoroyPlayRoom(Guid playRoomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);
        await playRoomActor.OnReqDestoryRoom(playRoomId);
    }

    public async Task Broadcast(Guid playRoomId, string message)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);

    }

}


