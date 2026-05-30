using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Actors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncnetPlatform.Interfaces.Actors;

public interface ILocalPlayer
{
    Guid PlayerId { get; }
    Task PingPong(int seq);
    Task<string> GetPlayerName();
    Task UpdatePlayerName(string newName);
    Task<PacketErrorCodes> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType);
    Task<Guid> CreateAndJoinPlayRoom(string roomName, bool isPrivate, int maxCapacity, string roomPassword);
    Task<PacketErrorCodes> JoinPlayRoom(Guid roomId);
    Task<List<PlayRoomMember>> GetPlayerListInPlayRoom(Guid roomId);
    Task<PacketErrorCodes> LeavePlayRoom(Guid roomId);
}
