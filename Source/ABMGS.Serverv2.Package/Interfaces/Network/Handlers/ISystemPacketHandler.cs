using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Network.Handlers;

public interface ISystemPacketHandler
{
    public Task BindPlayer(Guid playerId, WebSocket websocket);

}
