using System.Net.WebSockets;

namespace ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Sessions;

public interface IGameSessionService
{
    public Task StartGameSession(Guid uniquePlayerId, WebSocket webSocket, CancellationToken loopCancellationToken);
}