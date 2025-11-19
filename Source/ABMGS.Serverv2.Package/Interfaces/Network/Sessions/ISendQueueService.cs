using System.Net.WebSockets;


namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface ISendQueueService
{
    Task Register(Guid playerId, WebSocket webSocket);
}
