using ABMGS.ServerV2.Enums;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using Google.FlatBuffers;
using SyncnetPlatform.Dto;
using System.Runtime.CompilerServices;

namespace ABMGS.ServerV2.Grains;
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

public class FlatBufferParser
{
    public T Deserialize<T>(byte[] data) where T : 

}


public interface IGameSessionActor : IGrainWithGuidKey
{
    public Task StartGameLoop(string uniquePlayerId, WebSocket webSocket, CancellationToken cancellationToken);
}

public class GameSessionActor : Grain, IGameSessionActor 
{
    private readonly ILogger<GameSessionActor> _logger;
    private readonly IClusterClient _clusterClient;

    public GameSessionActor(ILogger<GameSessionActor> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task StartGameLoop(string uniquePlayerId, WebSocket SocketObject, CancellationToken abnormalExitToken)
    {
        IPlayerActor playerActor = _clusterClient.GetGrain<IPlayerActor>(new Guid(uniquePlayerId));
        FlatBufferParser parser = new FlatBufferParser();

        bool IsGameLoopValid = true;

        using (NetworkBuffer NBuf = new(4096))
        {
            //Loop to receive data from the WebSocket
            while (IsGameLoopValid && !abnormalExitToken.IsCancellationRequested)
            {
                while (true)
                {
                    ValueWebSocketReceiveResult result = await SocketObject.ReceiveAsync(NBuf.GetReceiveBuffer(), abnormalExitToken);
                    NBuf.AddBuffer(result.Count);

                    if (result.EndOfMessage == true)
                    {
                        await NBuf.FinishReceived();
                        break;
                    }
                }
            }

            // Get the received data
            byte[] receivedData = await NBuf.Read();

            parser.Deserialize<T>(receivedData);


            PacketWrapper packetWrapper = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(receivedData));
            switch (packetWrapper.SystemPacketType)
            {
                case SystemPacket.LoginRequest:
                    var loginRequest = packetWrapper.SystemPacketAsLoginRequest();
                    _logger.LogInformation("Received LoginRequest with Username: {Username}", loginRequest.Id);
                    break;
                case SystemPacket.MoveRequest:
                    MoveRequest moveRequest = packetWrapper.SystemPacketAsMoveRequest();
                    _logger.LogInformation("Received MoveRequest with Direction: {Direction}", moveRequest.X);
                    break;
                default:
                    _logger.LogWarning("Received unknown packet type: {PacketType}", packetWrapper.SystemPacketType);
                    break;
            }



            await Task.CompletedTask;
    }
}

public interface IFlatBufferSerializer : IGrainWithGuidKey
{
    public 
}


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
                        await NBuf.FinishReceived();
                        break;
                    }
                }
            }

            // Get the received data
            byte[] receivedData = await NBuf.Read();
            
            PacketWrapper packetWrapper = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(receivedData));
            switch(packetWrapper.SystemPacketType)
            {
                case SystemPacket.LoginRequest:
                    var loginRequest = packetWrapper.SystemPacketAsLoginRequest();
                    _logger.LogInformation("Received LoginRequest with Username: {Username}", loginRequest.Id);
                    break;
                case SystemPacket.MoveRequest:
                    MoveRequest moveRequest = packetWrapper.SystemPacketAsMoveRequest();
                    _logger.LogInformation("Received MoveRequest with Direction: {Direction}", moveRequest.X);
                    break;
                default:
                    _logger.LogWarning("Received unknown packet type: {PacketType}", packetWrapper.SystemPacketType);
                    break;
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




