using SyncnetPlatform.Controllers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerActor : IGrainWithGuidKey
{
    Task<Guid> CreateAndJoinPlayRoom(string roomName, bool isPrivate, int maxCapacity, string roomPassword);
    public Task Echo(int seq);
    Task<string> GetPlayerName();
    Task<PacketErrorCodes> JoinPlayRoom(Guid playRoomId);
    Task<PacketErrorCodes> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType);
    Task<PacketErrorCodes> OnPlayerJoinRoom(Guid roomId, Guid playerId, string playerName);
    Task PingPong(int seq);
    Task<PacketErrorCodes> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType);
    Task SetIdProvider(SupportedPlatformType idpFrom);
    Task SetOnline(bool isOnline);
    Task UpdatePlayerName(string newName);
}


