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

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepository playerModelRepository
        )
    {
        _logger = logger;
        _playerModelRepository = playerModelRepository;
        
    }

    public Task SetOnline(bool isOnline)
    {
        _IsOnline = isOnline;
        if(isOnline == true )
        {
            // Get activated ISendDataGrain.
            _packetSender = GrainFactory.GetGrain<ISendDataGrain>(GrainContext.GrainId.GetGuidKey());
        }
        else
        {
            _packetSender = null;
        }
        return Task.CompletedTask;
    }
    public async Task SetIdProvider(SupportedPlatformType idpFrom)
    {
        _idpFrom = idpFrom;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Guid PlayerId = GrainContext.GrainId.GetGuidKey();
        _playerData = await _playerModelRepository.GetOrCreate(PlayerId);
        _dbid = _playerData.Id;
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await _playerModelRepository.Update(_playerData);
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

    public async Task<bool> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType)
    {
        IPlayerActor targetPlayer = GrainFactory.GetGrain<IPlayerActor>(toPlayerId);
        bool result = await targetPlayer.OnDirectDeliveryData(GrainContext.GrainId.GetGuidKey(), message, dataType);
        return result;
    }
    public async Task<bool> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType)
    {
        if(!_IsOnline)
        {
            return false;
        }
        
        await _packetSender!.Send(
            PacketBuilder.Build<OnDirectDeliveryDataArgs>(
                new OnDirectDeliveryDataArgs(fromPlayerId, message, dataType)));
        return true;
    }

    public async Task<Guid> CreateAndJoinPlayRoom()
    {
        Guid newPlayRoomId = Guid.NewGuid();
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(newPlayRoomId);
        await playRoomActor.OnPlayerJoin(this.GetGrainId().GetGuidKey());
        return newPlayRoomId;
    }

    public async Task JoinPlayRoom(Guid playRoomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);
        await playRoomActor.OnPlayerJoin(GrainContext.GrainId.GetGuidKey());
    }

    public async Task LeavePlayRoom(Guid playRoomId)
    {
        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(playRoomId);

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


