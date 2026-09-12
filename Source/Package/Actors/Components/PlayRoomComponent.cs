using Microsoft.Extensions.Logging;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Actors.Components;

public interface IPlayRoomComponent
{
    Task<(PacketErrorCodes, byte[]?)> CreatePlayRoom(Guid newPlayRoomId, string roomName, bool isPrivate, int maxCapacity, string roomPassword);
    Task<(PacketErrorCodes, byte[])> JoinPlayRoom(Guid roomId);
    Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId);
    Task<PacketErrorCodes> LeavePlayRoom(Guid roomId);
    bool IsAlreadyInRoom(Guid roomId);
    Task<PacketErrorCodes> PlayerActionToPlayRoom(Guid roomId, Guid playerId, string actionType, byte[] actionParameters);
    void SetPlayerCustomBehavior(IPlayerCustomBehavior? playerCustomBehavior);
}

public class PlayRoomComponent(
    ILogger<PlayRoomComponent> logger, 
    IGrainFactory grainFactory, 
    Guid playerId, 
    PlayerState playerState) : IPlayRoomComponent
{
    private readonly List<Guid> _joinedRoomList = new List<Guid>();
    private IPlayerCustomBehavior? _playerCustomBehavior = null;


    public void SetPlayerCustomBehavior(IPlayerCustomBehavior? playerCustomBehavior)
    {
        _playerCustomBehavior = playerCustomBehavior;
    }


    public async Task<(PacketErrorCodes, byte[]?)> CreatePlayRoom(Guid newPlayRoomId, string roomName, bool isPrivate, int maxCapacity, string roomPassword)
    {
        #region Early exit check
        if(_joinedRoomList.Contains(newPlayRoomId)) return (PacketErrorCodes.AlreadyInRoom, []);
        #endregion 
        
        IPlayRoomActor newPlayRoomActor = grainFactory.GetGrain<IPlayRoomActor>(newPlayRoomId);

        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        byte[]? serializedPlayRoomState = null;

        (errorCode, serializedPlayRoomState) = await newPlayRoomActor.Create(
            roomName, 
            isPrivate, 
            maxCapacity, 
            roomPassword, 
            BuildPlayerRoomMember(newPlayRoomId));

        if (errorCode == PacketErrorCodes.Success)
        {
            _joinedRoomList.Add(newPlayRoomId);
        }
        
        logger.LogInformation("new room[{roomId}] created by Player[{playerId}]", newPlayRoomId.ToString(), playerId.ToString());
        return (errorCode, serializedPlayRoomState);
    }
    
    public async Task<(PacketErrorCodes, byte[])> JoinPlayRoom(Guid roomId)
    {
        #region Early exit check
        if (_joinedRoomList.Contains(roomId)) return (PacketErrorCodes.AlreadyInRoom, []);
        #endregion
        
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        IPlayRoomActor playRoomActor = grainFactory.GetGrain<IPlayRoomActor>(roomId);

        (errorCode, byte[] playRoomCustomState) = await playRoomActor.JoinPlayer(BuildPlayerRoomMember(roomId));
        if (errorCode == PacketErrorCodes.Success)
        {
            _joinedRoomList.Add(roomId);
        }
        return (errorCode, playRoomCustomState);
    }

    public async Task<PacketErrorCodes> PlayerActionToPlayRoom(Guid roomId, Guid playerId, string actionType, byte[] actionParameters)
    {
        if (!_joinedRoomList.Contains(roomId)) return PacketErrorCodes.YoureNotInTheRoom;
        
        IPlayRoomActor playRoomActor = grainFactory.GetGrain<IPlayRoomActor>(roomId);
        return await playRoomActor.ReqPlayerActionToPlayRoom(playerId, actionType, actionParameters);
    }
    
    
    public async Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId)
    {
        IPlayRoomActor playRoomActor = grainFactory.GetGrain<IPlayRoomActor>(roomId);
        List<PlayRoomMember> players = await playRoomActor.GetPlayersInPlayRoom();
        return players;
    }

    public async Task<PacketErrorCodes> LeavePlayRoom(Guid roomId)
    {
        IPlayRoomActor playRoomActor = grainFactory.GetGrain<IPlayRoomActor>(roomId);
        PacketErrorCodes result = await playRoomActor.LeavePlayer(BuildPlayerRoomMember(roomId));
        _joinedRoomList.Remove(roomId);

        return result;
    }

    public bool IsAlreadyInRoom(Guid roomId) => _joinedRoomList.Contains(roomId);


    private PlayRoomMember BuildPlayerRoomMember(Guid roomId)
    // {
    //     PlayRoomMember newOne = new();
    //     newOne.RoomId = roomId;
    //     newOne.PlayerId = playerId;
    //     newOne.PlayerName = playerState.PlayerName;
    //     newOne.PlayerExtendData =
    //         _playerCustomBehavior == null ? [] : _playerCustomBehavior.Serialize(playerState.Extension);
    //     newOne.PlayerStateForPlayRoom =
    //         _playerCustomBehavior == null?[] : _playerCustomBehavior.
    //
    // }
        => new PlayRoomMember(roomId, playerId, playerState.PlayerName, 
            _playerCustomBehavior == null ? [] : _playerCustomBehavior.Serialize(playerState.Extension));
}

