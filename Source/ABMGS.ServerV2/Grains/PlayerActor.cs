using System.Net.WebSockets;

namespace ABMGS.ServerV2.Grains;


public interface IPlayerActor : IGrainWithGuidKey
{
    public Task<INetworkReceiveActor> GetNetworkReceiveActor();
}
public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;
    }
    public Task<INetworkReceiveActor> GetNetworkReceiveActor()
    {
        string NetworkReceiveActorId = string.Join("/", this.GetGrainId().GetGuidKey().ToString(), "NetworkReceiveActor");
        return Task.FromResult(GrainFactory.GetGrain<INetworkReceiveActor>(NetworkReceiveActorId));
    }

}

public interface INetworkReceiveActor : IGrainWithStringKey
{

}
public interface INetworkParserActor : IGrainWithStringKey
{

}

public class NetworkReceiveActor : Grain, INetworkReceiveActor
{
    public void ReceivingLoop(WebSocket webSocket)
    {
        webSocket.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);

    }
}

public class NetworkParserActor : Grain, INetworkParserActor
{

}




