using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Actors;


interface IPlayRoomActor : IGrainWithGuidKey
{
    Task<bool> IsValidRoomToJoin();
    Task<bool> OnPlayerJoin(Guid playerId);
    Task OnPlayerLeave(Guid playerId);
    Task OnReqDestoryRoom(Guid roomId);
    Task SetRoomInformation(string displayName, bool isPrivate, int maxCapacity, string roomPassword, Guid roomOwnerPlayerId);
}

public class PlayRoomActor : Grain, IPlayRoomActor
{
    private readonly ILogger<PlayRoomActor> _logger;
    private List<Guid> players = new List<Guid>();

    private string _displayName = String.Empty;
    private string _passwordForEntrance = String.Empty;
    private int _maxPlayerCapacity = 4;
    private bool _isPrivate = false;
    private Guid _ownerPlayerId = Guid.Empty;
    public PlayRoomActor(ILogger<PlayRoomActor> logger)
    {
        _logger = logger;
    }

    public Guid RoomId {
        private set { }
        get { return GrainContext.GrainId.GetGuidKey();} 
    }
    /// <summary>
    /// Create new playroom and join the room owner automatically
    /// </summary>
    /// <param name="displayName"></param>
    /// <param name="isPrivate"></param>
    /// <param name="maxCapacity"></param>
    /// <param name="roomPassword"></param>
    /// <param name="roomOwnerPlayerId"></param>
    /// <returns></returns>
    public async Task SetRoomInformation(string displayName, bool isPrivate, int maxCapacity, string roomPassword, Guid roomOwnerPlayerId)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        _displayName = displayName;
        _passwordForEntrance = roomPassword;
        _maxPlayerCapacity = maxCapacity;
        _isPrivate = isPrivate;
        _ownerPlayerId = roomOwnerPlayerId;
        players.Add(roomOwnerPlayerId);
    }

    public Task<bool> IsValidRoomToJoin() => Task.FromResult(_ownerPlayerId !=  Guid.Empty);
        

    public async Task<bool> OnPlayerJoin(Guid playerId)
    {
        if (_ownerPlayerId == Guid.Empty)
        {
            return false;
        }

        foreach (var player in players)
        {
            IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(player);
            await p.OnPlayerJoinRoom(RoomId, playerId, "Guest");
        }

        players.Add(playerId);

        return true;
    }

    public async Task OnPlayerLeave(Guid playerId)
    {
        players.Remove(playerId);
        if(players.Count == 0)
        {
            base.DeactivateOnIdle();
        }
    }

    public async Task OnReqDestoryRoom(Guid roomId)
    {
        if( roomId.Equals(RoomId))
        {
            Init();
            base.DeactivateOnIdle();
        }
    }


    protected void Init()
    {
        players.Clear();
    }
}
