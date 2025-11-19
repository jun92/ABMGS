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
    private record SendQueueEntity(Guid PlayerId, byte[] SendData);
    private readonly IDictionary<Guid, Channel<byte[]>> _managedChannels = new Dictionary<Guid, Channel<byte[]>>();
    private readonly IDictionary<Guid, Task> _managedSendingTask = new  ConcurrentDictionary<Guid, Task>();
    public SendQueueService(ILogger<SendQueueService> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
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
        var observerRef = _clusterClient.CreateObjectReference<ISendDataObserver>(observer);
        var sendDataGrain = _clusterClient.GetGrain<ISendDataGrain>(playerId);
        await sendDataGrain.Register(observerRef);
    }
    public async Task Register(Guid playerId, WebSocket webSocket)
    {
        await RegisterSocket(playerId, webSocket);
        await RegisterObserver(playerId);
    }

    public void Unregister(Guid playerId)
    {
        _managedChannels.Remove(playerId);
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
            Unregister(playerId);
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
