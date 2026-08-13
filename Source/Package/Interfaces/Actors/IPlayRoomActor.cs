using SyncnetPlatform.Actors;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayRoomActor : IGrainWithGuidKey
{
    Task<List<PlayRoomMember>> GetPlayersInPlayRoom();
    Task<bool> IsValidRoomToJoin();
    Task<(PacketErrorCodes, byte[])> JoinPlayer(PlayRoomMember joiner);
    Task<PacketErrorCodes> LeavePlayer(PlayRoomMember leaver);
    Task<(PacketErrorCodes, byte[]?)> SetRoomInformation(string displayName, bool isPrivate, int maxCapacity, string roomPassword, PlayRoomMember owner);
    Task OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter);
}

