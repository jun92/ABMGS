using SyncnetPlatform.Actors;
using SyncnetPlatform.Controllers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerActor : IGrainWithGuidKey, IPacketHandlerActor
{
    Task<(Guid, IPlayRoomMetaData?)> CreateAndJoinPlayRoom(string roomName, bool isPrivate, int maxCapacity, string roomPassword);
    public Task Echo(int seq);
    Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId);
    Task<string> GetPlayerName();
    Task<PacketErrorCodes> JoinPlayRoom(Guid playRoomId);
    Task<PacketErrorCodes> LeavePlayRoom(Guid playRoomId);
    Task<PacketErrorCodes> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType);
    Task OnUpdateForPlayRoomMembers(PlayRoomMember playRoomMember, PlayRoomMemberUpdateReason memberStatus);
    Task PingPong(int seq);
    Task<PacketErrorCodes> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType);
    Task SetIdProvider(SupportedPlatformType idpFrom);
    Task SetOnline(bool isOnline);
    Task UpdatePlayerName(string newName);
    Task OnHandleCustomPacket(byte[] customPacket);
}


