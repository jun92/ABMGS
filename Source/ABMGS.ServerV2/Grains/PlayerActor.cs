using System.Net.WebSockets;

namespace ABMGS.ServerV2.Grains;


public interface IPlayerActor : IGrainWithGuidKey
{

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

    }
}

public class NetworkParserActor : Grain, INetworkParserActor
{

}



public class PlayerActor : Grain, IPlayerActor
{
    private ILogger<PlayerActor> _logger;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;
    }
    public INetworkReceiveActor GetNetworkReceiveActor() => GrainFactory.GetGrain<INetworkReceiveActor>(string.Join("/", this.GetGrainId().GetGuidKey().ToString(), "NetworkReceiveActor"));

}

