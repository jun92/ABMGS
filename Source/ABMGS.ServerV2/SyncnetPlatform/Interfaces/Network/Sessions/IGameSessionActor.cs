using System.Net.WebSockets;

namespace ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Sessions;

public interface IGameSessionActor : IGrainWithGuidKey
{
    public Task StartGameLoop(string uniquePlayerId, WebSocket webSocket, CancellationToken cancellationToken);
}
