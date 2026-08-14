using SyncnetPlatform.Actors;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Interfaces.Actors;

[Alias("SyncnetPlatform.Interfaces.Actors.IPlayRoomActor")]
public interface IPlayRoomActor : IGrainWithGuidKey
{
    [Alias("GetPlayersInPlayRoom")]
    Task<List<PlayRoomMember>> GetPlayersInPlayRoom();
    [Alias("IsValidRoomToJoin")]
    Task<bool> IsValidRoomToJoin();
    [Alias("JoinPlayer")]
    Task<(PacketErrorCodes, byte[])> JoinPlayer(PlayRoomMember joiner);
    [Alias("LeavePlayer")]
    Task<PacketErrorCodes> LeavePlayer(PlayRoomMember leaver);
    [Alias("SetRoomInformation")]
    Task<(PacketErrorCodes, byte[]?)> SetRoomInformation(string displayName, bool isPrivate, int maxCapacity, string roomPassword, PlayRoomMember owner);
    [Alias("OnPlayerActionToPlayRoom")]
    Task OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter);
}

