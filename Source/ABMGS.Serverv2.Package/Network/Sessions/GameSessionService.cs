using Google.FlatBuffers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace SyncnetPlatform.Network.Sessions;


public interface ISendDataObserver : IGrainObserver
{
    Task SendDataAsync(Guid playerId, byte[] data);
}

public class SendDataObserver : ISendDataObserver
{
    private readonly SendQueueService _sendQueueService;
    private readonly Guid _playerId;
    public SendDataObserver(SendQueueService service, Guid PlayerId)
    {
        _sendQueueService = service;
        _playerId = PlayerId;
    }

    public async Task SendDataAsync(Guid playerId, byte[] data)
    {
        await _sendQueueService.SendDataAsync(playerId, data);
    }
}


public interface ISendDataGrain : IGrainWithGuidKey
{
    Task Register(ISendDataObserver observer);
    Task Send(Guid playerId, byte[] data);
}

public class SendDataGrain : Grain, ISendDataGrain
{
    private ISendDataObserver? _sendDataObserver = null;
    public async Task Register(ISendDataObserver observer)
    {
        _sendDataObserver = observer;
    }

    public async Task Send(Guid playerId, byte[] data)
    {
        if(_sendDataObserver is not null)
        {
            await _sendDataObserver.SendDataAsync(playerId, data);
        }
    }
}
public class SendQueueService : BackgroundService, ISendDataObserver
{
    private record SendQueueEntity(Guid PlayerId, byte[] SendData);
    private readonly ILogger<SendQueueService> _logger;
    private readonly IDictionary<Guid, WebSocket> _managedSockets = new ConcurrentDictionary<Guid, WebSocket>();
    private readonly ConcurrentQueue<SendQueueEntity> _sendQueue = new ConcurrentQueue<SendQueueEntity>();

    //private readonly IGrainFactory _grainFactory;
    //private readonly IGrainRuntime _grainRuntime;
    private readonly IClusterClient _clusterClient;
    public SendQueueService(ILogger<SendQueueService> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        //_grainFactory = grainFactory;
        //_grainRuntime = grainRuntime;
    }

    protected async Task RegisterSocket(Guid playerId, WebSocket webSocket)
    {
        _managedSockets.Add(playerId, webSocket);

    }
    protected async Task RegisterObserver(Guid playerId)
    {
        var observer = new SendDataObserver(this, playerId);
        var observerRef = _clusterClient.CreateObjectReference<ISendDataObserver>(observer);
        var sendDataGrain = _clusterClient.GetGrain<ISendDataGrain>(playerId);
        await sendDataGrain.Register(observerRef);
    }
    public async Task Register(Guid playerId, WebSocket webSocket)
    {
        await RegisterSocket(playerId, webSocket);
        await RegisterObserver(playerId);
    }

    public async Task SendOverSocketAsync(Guid playerId, byte[] payload)
    {
        if (!_managedSockets.TryGetValue(playerId, out var socket))
        {
            _logger.LogError("Connection moved or not found");
            return; // connection lost or moved
        }

        if (socket.State != WebSocketState.Open)
            return;

        await socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    public void Push(Guid playerId, byte[] data)
    {
        _sendQueue.Enqueue(new SendQueueEntity(playerId, data));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_sendQueue.TryDequeue(out var sendQueue))
            {
                await SendOverSocketAsync(sendQueue.PlayerId, sendQueue.SendData);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
     
    }

    public async Task SendDataAsync(Guid playerId, byte[] data)
    {
        Push(playerId,data);
    }

    //public async Task StartAsync(CancellationToken cancellationToken)
    //{
    //}

    //public Task StopAsync(CancellationToken cancellationToken)
    //{
    //    return Task.CompletedTask;
    //}
}

public class GameSessionService : IGameSessionService
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly FlatBufferPacketRouter _routeTable;
   
    private readonly SendQueueService _sendQueue;
    private readonly ICustomPacketHandler _customPacketHandler;
    private readonly SystemPacketHandler _systemPacketHandler;

    public GameSessionService(
        ILogger<GameSessionService> logger, 
        IClusterClient clusterClient, 
        SendQueueService sendQueue,
        ICustomPacketHandler customPacketHandler,
        SystemPacketHandler systemPacketHandler,
        FlatBufferPacketRouter routeTable)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _routeTable = routeTable;
        _sendQueue = sendQueue;
        _customPacketHandler = customPacketHandler;
        _systemPacketHandler = systemPacketHandler;
    }

    public async Task StartGameSession(Guid uniquePlayerId, WebSocket SocketObject, CancellationToken abnormalExitToken)
    {
        ArgumentNullException.ThrowIfNull(SocketObject);

        IPacketHandler packetHandlingActor = _clusterClient.GetGrain<IPacketHandler>(uniquePlayerId);

        await _sendQueue.Register(uniquePlayerId, SocketObject);

        bool IsGameLoopValid = true;

        //Loop to receive data from the WebSocket
        while (IsGameLoopValid && !abnormalExitToken.IsCancellationRequested)
        {
            using (NetworkBuffer NBuf = new(4096))
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
                await packetHandlingActor.PushRecievedData(await NBuf.Read());
                // _routeTable.Execute(PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(await NBuf.Read())));
            }
        }
        await Task.CompletedTask;
    }
}
