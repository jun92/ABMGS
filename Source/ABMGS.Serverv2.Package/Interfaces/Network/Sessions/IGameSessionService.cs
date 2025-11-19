using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface IGameSessionService
{
    public Task StartGameSession(Guid uniquePlayerId, WebSocket webSocket);
}