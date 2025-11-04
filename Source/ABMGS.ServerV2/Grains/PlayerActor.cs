using ABMGS.ServerV2.Enums;
using System.IO.Pipelines;
using System.Net.WebSockets;

namespace ABMGS.ServerV2.Grains;

public interface IPlayerActor : IGrainWithGuidKey
{
    public Task StartGameLoop(WebSocket SocketHandle, string UniquePlayerId, CancellationToken AbnormalExitToken);
    // public Task<INetworkReceiveActor> GetNetworkReceiveActor();
}
public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;
    }
    public async Task StartGameLoop(WebSocket SocketHandle, string UniquePlayerId, CancellationToken AbnormalExitToken)
    {
        #region Validations
        ArgumentNullException.ThrowIfNullOrEmpty(UniquePlayerId);
        ArgumentNullException.ThrowIfNull(SocketHandle);
        #endregion

        bool IsGameLoopValid = true;    

        Pipe pipe = new Pipe();
        PipeWriter writer = pipe.Writer;


        //Loop to receive data from the WebSocket
        while(IsGameLoopValid)
        {
            while(true)
            {
                Memory<byte> receiveBuffer = writer.GetMemory(4096);

                ValueWebSocketReceiveResult result = await SocketHandle.ReceiveAsync(receiveBuffer, AbnormalExitToken);
                writer.Advance(result.Count);
                if(result.EndOfMessage == true)
                {
                    await writer.CompleteAsync();
                    break;
                }
            }
        }
    }
    public Task<INetworkReceiveActor> GetNetworkReceiveActor()
    {
        string NetworkReceiveActorId = string.Join("/", this.GetGrainId().GetGuidKey().ToString(), ActorSuffixNames.NetworkReceiveActor);
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




