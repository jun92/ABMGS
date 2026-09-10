using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Actors.Components;

public interface IPlayRoomComponent : IPlayRoomComponent
{
    Task<(PacketErrorCodes, byte[]?)> CreatePlayRoom(Guid newPlayRoomId, string roomName, bool isPrivate, int maxCapacity, string roomPassword);
}

public class PlayRoomComponent(ILogger<PlayRoomComponent> logger, IGrainFactory grainFactory, Guid playerId, PlayerState playerState) : IPlayRoomComponent
{
    private readonly ILogger<PlayRoomComponent> _logger = logger;
    private readonly List<Guid> _joinedRoomList = new List<Guid>();
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly Guid _playerId = playerId;
    private readonly PlayerState _playerState = playerState;


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
    protected PlayRoomMember BuildPlayerRoomMember(Guid roomId) 
        => new PlayRoomMember(roomId, _playerId, _playerState.PlayerName, SerializePlayerExtendData());
}

