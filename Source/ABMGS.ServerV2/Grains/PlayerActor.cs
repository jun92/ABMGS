using ABMGS.ServerV2.Enums;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace ABMGS.ServerV2.Grains;

public interface IPlayerActor : IGrainWithGuidKey
{
    public Task StartGameLoop(WebSocket SocketHandle, string UniquePlayerId, CancellationToken AbnormalExitToken);
    // public Task<INetworkReceiveActor> GetNetworkReceiveActor();
}

public class NetworkBuffer : IDisposable
{
    private readonly Pipe _pipe;
    private readonly int _bufferSize;

    public NetworkBuffer(int bufferSize)
    {
        _pipe = new Pipe();
        _bufferSize = bufferSize;
    }

    public Memory<byte> GetReceiveBuffer() => _pipe.Writer.GetMemory(_bufferSize);
    public void AddBuffer(int receivedByteCount) => _pipe.Writer.Advance(receivedByteCount);
    public async Task FinishReceived()
    {
        FlushResult result = await _pipe.Writer.FlushAsync();
        await _pipe.Writer.CompleteAsync();
    }

    public async Task<byte[]> Read()
    {
        ReadResult readResult = await _pipe.Reader.ReadAsync();
        return readResult.Buffer.ToArray<byte>();

    }
    public void Dispose()
    {
        _pipe.Reset();
    }
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

        using (NetworkBuffer NBuf = new NetworkBuffer(4096))
        {
            //Loop to receive data from the WebSocket
            while (IsGameLoopValid && !AbnormalExitToken.IsCancellationRequested)
            {
                while (true)
                {
                    ValueWebSocketReceiveResult result = await SocketHandle.ReceiveAsync(NBuf.GetReceiveBuffer(), AbnormalExitToken);
                    NBuf.AddBuffer(result.Count);

                    if (result.EndOfMessage == true)
                    {
                        NBuf.FinishReceived();
                        break;
                    }
                }
            }
        }
    }
    public Task<INetworkReceiveActor> GetNetworkReceiveActor()
    {
        string NetworkReceiveActorId = string.Join("/", this.GetGrainId().GetGuidKey().ToString(), ActorSuffixNames.NetworkReceiveActor.ToString());
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




