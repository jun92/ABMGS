using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Actors.Player;

public interface IPlayerActor : IGrainWithGuidKey
{
    public Task Initialize(WebSocket webSocket);
    public Task Echo(int seq);
}


