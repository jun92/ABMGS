using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Actors;



public interface IPlayRoomActor : IGrainWithGuidKey
{
    Task<bool> IsValidRoomToJoin();
    Task<PacketErrorCodes> JoinPlayer(PlayRoomMember joiner);
    Task<PacketErrorCodes> LeavePlayer(PlayRoomMember leaver);
    Task OnReqDestoryRoom(Guid roomId);
    Task SetRoomInformation(string displayName, bool isPrivate, int maxCapacity, string roomPassword, PlayRoomMember owner);
}

public class PlayRoomActor : Grain, IPlayRoomActor
{
    private readonly ILogger<PlayRoomActor> _logger;
    private List<PlayRoomMember> players = new List<PlayRoomMember>();

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
    public async Task SetRoomInformation(
        string displayName, 
        bool isPrivate, 
        int maxCapacity, 
        string roomPassword, PlayRoomMember owner)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        _displayName = displayName;
        _passwordForEntrance = roomPassword;
        _maxPlayerCapacity = maxCapacity;
        _isPrivate = isPrivate;
        _ownerPlayerId = owner.PlayerId;
        players.Add(owner);
    }

    public Task<bool> IsValidRoomToJoin() => Task.FromResult(_ownerPlayerId !=  Guid.Empty);
        

    public async Task<PacketErrorCodes> JoinPlayer(PlayRoomMember joiner)
    {
        if (_ownerPlayerId == Guid.Empty)
        {
            return PacketErrorCodes.RoomNotFound;
        }

        foreach (var player in players)
        {
            IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(player.PlayerId);
            await p.OnUpdateForPlayRoomMembers(joiner, PlayRoomMemberUpdate.Join);
        }

        players.Add(joiner);

        return PacketErrorCodes.Success;
    }

    public async Task<PacketErrorCodes> LeavePlayer(PlayRoomMember leaver)
    {
        if(_ownerPlayerId == Guid.Empty)
        {
            return PacketErrorCodes.RoomNotFound;
        }
        if(leaver.PlayerId == _ownerPlayerId) // in case of the owner of the room leaving.
        {
            //Should destory this room.

        }
        else
        {
            foreach(var player in players)
            {
                IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(player.PlayerId);
                await p.OnUpdateForPlayRoomMembers(leaver, PlayRoomMemberUpdate.Leave);
            }
        }
        players.Remove(leaver);
        if(players.Count == 0)
        {
            base.DeactivateOnIdle();
        }
        return PacketErrorCodes.Success;
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
