using SyncnetPlatform.Actors;
using SyncnetPlatform.Controllers;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Interfaces.Actors;

[Alias("SyncnetPlatform.Interfaces.Actors.IPlayerActor")]
public interface IPlayerActor : IGrainWithGuidKey, IPacketHandlerActor
{
    [Alias("CreateAndJoinPlayRoom")]
    Task<(PacketErrorCodes, Guid, byte[]?)> CreateAndJoinPlayRoom(string roomName, bool isPrivate, int maxCapacity, string roomPassword, byte[] playerMetadata);
    
    [Alias("Echo")]
    public Task Echo(int seq);
    
    [Alias("GetPlayerListInPlayRoom")]
    Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId);
    
    [Alias("GetPlayerName")]
    Task<string> GetPlayerName();
    
    [Alias("JoinPlayRoom")]
    Task<(PacketErrorCodes, byte[])> JoinPlayRoom(Guid playRoomId);
    
    [Alias("LeavePlayRoom")]
    Task<PacketErrorCodes> LeavePlayRoom(Guid playRoomId);
    
    [Alias("OnDirectDeliveryData")]
    Task<PacketErrorCodes> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType);
    
    [Alias("OnUpdateForPlayRoomMembers")]
    ValueTask OnUpdateForPlayRoomMembers(PlayRoomMember playRoomMember, PlayRoomMemberUpdateReason memberStatus);

    [Alias("OnUpdatePlayerExtendData")]
    ValueTask OnUpdatePlayerExtendData(byte[] extendData);
    
    [Alias("PingPong")]
    Task PingPong(int seq);
    
    [Alias("SendDirectDeliverData")]
    Task<PacketErrorCodes> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType);
    
    [Alias("SetIdProvider")]
    ValueTask SetIdProvider(SupportedPlatformType idpFrom);
    
    [Alias("SetOnline")]
    ValueTask SetOnline(bool isOnline);
    
    [Alias("UpdatePlayerName")]
    Task UpdatePlayerName(string newName);
    
    [Alias("OnHandleCustomPacket")]
    Task OnHandleCustomPacket(byte[] customPacket);
    
    [Alias("OnUpdatePlayRoomCustomState")]
    ValueTask OnUpdatePlayRoomCustomState(Guid roomId, byte[] customState);
}


