using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Network.Sessions;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Channels;


namespace SyncnetPlatform.Network.Sessions;

public class SendQueueService : BackgroundService, ISendDataObserver, ISendQueueService
{
    private readonly ILogger<SendQueueService> _logger;
    private readonly IClusterClient _clusterClient;
    protected readonly IDictionary<Guid, ISendDataObserver> _sendDataObservers = new ConcurrentDictionary<Guid, ISendDataObserver>();
    protected CancellationTokenSource? _sendExceptionToken;
    private readonly IDictionary<Guid, Channel<byte[]>> _managedChannels = new ConcurrentDictionary<Guid, Channel<byte[]>>();
    private readonly IDictionary<Guid, Task> _managedSendingTask = new  ConcurrentDictionary<Guid, Task>();
    public SendQueueService(ILogger<SendQueueService> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task Register(Guid playerId, WebSocket webSocket, CancellationTokenSource sendExceptionToken)
    {
        await RegisterSocket(playerId, webSocket);
        await RegisterObserver(playerId);
        _sendExceptionToken = sendExceptionToken;
    }
    protected async Task RegisterSocket(Guid playerId, WebSocket webSocket)
    {
        var newChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
            Capacity = 100 // TODO: Should go to config something later.
        });
        _managedChannels.Add(playerId, newChannel);
        _managedSendingTask.Add(playerId, Task.Run(() => PlayerSendLoop(playerId, webSocket, newChannel)));
    }
    protected async Task RegisterObserver(Guid playerId)
    {
        var observer = new SendDataObserver(this, playerId);
        var sendDataObserverRef = _clusterClient.CreateObjectReference<ISendDataObserver>(observer);
        _sendDataObservers.Add(playerId, sendDataObserverRef);
        var sendDataGrain = _clusterClient.GetGrain<ISendDataGrain>(playerId);
        await sendDataGrain.Register(sendDataObserverRef);
    }

    public async Task Unregister(Guid playerId)
    {
        if (!_managedChannels.ContainsKey(playerId)) return;
        await UnregisterSockets(playerId);
        await UnregisterObserver(playerId);
    }
    protected async Task UnregisterObserver(Guid playerId)
    {
        if(_sendDataObservers.TryGetValue(playerId, out var observerRef))
        {
            _clusterClient.DeleteObjectReference<ISendDataObserver>(observerRef);
            _sendDataObservers.Remove(playerId);
        }
        var sendDataGrain = _clusterClient.GetGrain<ISendDataGrain>(playerId);
        await sendDataGrain.Unregister();
    }

    protected async Task UnregisterSockets(Guid playerId)
    {
        if(_managedChannels.TryGetValue(playerId, out var channel))
        {
            channel.Writer.TryComplete();
            _managedChannels.Remove(playerId);
        }
        _managedSendingTask.Remove(playerId);
    }

    public async Task PlayerSendLoop(Guid playerId, WebSocket webSocket, Channel<byte[]> channel)
    {
        try
        {
            await foreach (var payload in channel.Reader.ReadAllAsync())
            {
                if (webSocket.State != WebSocketState.Open) break;
                await webSocket.SendAsync(
                    new ArraySegment<byte>(payload), 
                    WebSocketMessageType.Binary, 
                    true, 
                    CancellationToken.None); // Later change to some valuable cancellation token.
            }
        }
        finally
        {
            await Unregister(playerId);
            _sendExceptionToken?.Cancel();
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000);
        }
    }

    public async Task SendDataAsync(Guid playerId, byte[] data)
    {
        if(_managedChannels.TryGetValue(playerId, out var channel))
        {
            await channel.Writer.WriteAsync(data);
        }
        else
        {
            _logger.LogError($"Not found PlayerId({playerId}) in SendDataAsync");
        }
    }
}
