using Microsoft.Extensions.Logging;
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
}

public class PlayRoomComponent(
    ILogger<PlayRoomComponent> logger, 
    IGrainFactory grainFactory, 
    Guid playerId, 
    PlayerState playerState,
    IPlayerCustomBehavior? playerCustomBehavior) : IPlayRoomComponent
{
    private readonly ILogger<PlayRoomComponent> _logger = logger;
    private readonly List<Guid> _joinedRoomList = new List<Guid>();
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly Guid _playerId = playerId;
    private readonly PlayerState _playerState = playerState;
    private readonly IPlayerCustomBehavior? _playerCustomBehavior = playerCustomBehavior;


    public async Task<(PacketErrorCodes, byte[]?)> CreatePlayRoom(Guid newPlayRoomId, string roomName, bool isPrivate, int maxCapacity, string roomPassword)
    {
        #region Early exit check
        // if (!_IsOnline) return (PacketErrorCodes.PlayerOffline, []);
        if (_joinedRoomList.Exists(e => e.Equals(newPlayRoomId))) return (PacketErrorCodes.AlreadyInRoom, []);
        #endregion 
        
        IPlayRoomActor newPlayRoomActor = _grainFactory.GetGrain<IPlayRoomActor>(newPlayRoomId);

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
        return (errorCode, serializedPlayRoomState);
    }
    
    public async Task<(PacketErrorCodes, byte[])> JoinPlayRoom(Guid roomId)
    {
        #region Early exit check
        // if(!_IsOnline) return (PacketErrorCodes.PlayerOffline, []);
        if (_joinedRoomList.Exists(e => e.Equals(roomId))) return (PacketErrorCodes.AlreadyInRoom, []);
        #endregion
        
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        IPlayRoomActor playRoomActor = _grainFactory.GetGrain<IPlayRoomActor>(roomId);

        (errorCode, byte[] playRoomCustomState) = await playRoomActor.JoinPlayer(BuildPlayerRoomMember(roomId));
        if (errorCode == PacketErrorCodes.Success)
        {
            _joinedRoomList.Add(roomId);
        }
        return (errorCode, playRoomCustomState);
    }
    
    
    public async Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId)
    {
        IPlayRoomActor playRoomActor = _grainFactory.GetGrain<IPlayRoomActor>(roomId);
        List<PlayRoomMember> players = await playRoomActor.GetPlayersInPlayRoom();
        return players;
    }

    public async Task<PacketErrorCodes> LeavePlayRoom(Guid roomId)
    {
        IPlayRoomActor playRoomActor = _grainFactory.GetGrain<IPlayRoomActor>(roomId);
        PacketErrorCodes result = await playRoomActor.LeavePlayer(BuildPlayerRoomMember(roomId));
        _joinedRoomList.Remove(roomId);

        return result;
    }

    public bool IsAlreadyInRoom(Guid roomId) => _joinedRoomList.Contains(roomId);
    
    
    private PlayRoomMember BuildPlayerRoomMember(Guid roomId) 
        => new PlayRoomMember(roomId, _playerId, _playerState.PlayerName, SerializePlayerExtendData());

    private byte[] SerializePlayerExtendData()
        => _playerCustomBehavior != null
            ? _playerCustomBehavior.GetPlayerCustomState().Serialize(_playerState.Extension)
            : [];

    
}

