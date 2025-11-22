using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace SyncnetPlatform.Network.Sessions;

public class GameSessionService : IGameSessionService, ISendDataObserver
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IClusterClient _clusterClient;

    private readonly Channel<byte[]> _sendingQueueChannel;
    private ISendDataObserver? _sendDataObserver;
    private Task? _sendLoopTask;

    public GameSessionService(
        ILogger<GameSessionService> logger, 
        IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;

        _sendingQueueChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
            Capacity = 100 // TODO: Should go to config something later.
        });
        _sendDataObserver = null;
    }

    public async Task SendDataAsync(byte[] data)
    {
        await _sendingQueueChannel.Writer.WriteAsync(data);
    }

    protected async Task SendDataLoop(
        WebSocket socket, 
        Channel<byte[]> channel, 
        CancellationToken sendLoopExitToken)
    {
        try
        {
            await foreach (var payload in channel.Reader.ReadAllAsync(sendLoopExitToken))
            {
                if (socket.State != WebSocketState.Open) break;
                await socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Binary,
                    true,
                    sendLoopExitToken);
            }
        }
        catch(WebSocketException ex)
        {
            _logger.LogError(ex, "Socket operation error in SendAsync");
        }
        catch(OperationCanceledException ex)
        {
            //Triggered by on purpose.
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Exception in Sending Loop");
        }
    }
    protected void RunSendingLoopTask(WebSocket socket, CancellationToken sendLoopExitToken)
    {
        _sendLoopTask = Task.Run(() => SendDataLoop(socket, _sendingQueueChannel, sendLoopExitToken));
    }

    protected async Task RegisterObserverForSendDataEvent(Guid playerId)
    {
        var sendObserver = new SendDataObserver(this);
        _sendDataObserver = _clusterClient.CreateObjectReference<ISendDataObserver>(sendObserver);
        var sendDataGrain = _clusterClient.GetGrain<ISendDataGrain>(playerId);
        await sendDataGrain.Register(_sendDataObserver);
    }
    protected async Task UnregisterObserver(Guid playerId)
    {
        var sendDataGrain = _clusterClient.GetGrain<ISendDataGrain>(playerId);
        await sendDataGrain.Unregister();
        if (_sendDataObserver is { } sdo) _clusterClient.DeleteObjectReference<ISendDataObserver>(sdo);
        _sendDataObserver = null;

    }
    protected async Task RunGameLoop(Guid playerId, WebSocket SocketObject, CancellationToken mainLoopExitToken)
    {
        IPacketHandler packetHandlingActor = _clusterClient.GetGrain<IPacketHandler>(playerId);

        while (!mainLoopExitToken.IsCancellationRequested)
        {
            using NetworkBuffer NBuf = new(4096);
            while (true)
            {
                ValueWebSocketReceiveResult result;
                try
                {
                    result = await SocketObject.ReceiveAsync(NBuf.GetReceiveBuffer(), mainLoopExitToken);
                }
                catch(OperationCanceledException)
                {
                    // Exit with nothing wrong
                    return;
                }
                NBuf.AddBuffer(result.Count);

                if (result.MessageType == WebSocketMessageType.Close || result.Count == 0)
                {
                    try
                    {
                        await SocketObject.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Socket Closed",
                            mainLoopExitToken);
                    }
                    catch(WebSocketException ex)
                    {
                        _logger.LogError(ex, $"WebSocket exception while closing it: {nameof(RunGameLoop)}");
                    }
                    return;
                }

                if (result.EndOfMessage)
                {
                    await NBuf.FinishReceived();
                    _ = packetHandlingActor.PushRecievedData(await NBuf.Read());
                    break;
                }
            }
        }
    }
    public async Task StartGameSession(Guid uniquePlayerId, WebSocket SocketObject)
    {
        ArgumentNullException.ThrowIfNull(SocketObject);
        if (uniquePlayerId == Guid.Empty) 
            throw new ArgumentException("PlayerId is empty", nameof(uniquePlayerId));

        using var mainLoopExitTokenCts = new CancellationTokenSource();
        var mainLoopExitToken = mainLoopExitTokenCts.Token;
        
        try
        {
            await RegisterObserverForSendDataEvent(uniquePlayerId);
            RunSendingLoopTask(SocketObject, mainLoopExitToken);
            await RunGameLoop(uniquePlayerId, SocketObject, mainLoopExitToken);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error in GameSessionLoop");
        }
        
        finally
        {
            await FlushSendQueue();
            await UnregisterObserver(uniquePlayerId);
            await ShutdownSocket(SocketObject);
        }
    }

    protected async Task FlushSendQueue()
    {
        _sendingQueueChannel?.Writer.TryComplete();
        if (_sendLoopTask is not null)
        {
            await _sendLoopTask;
        }
    }
    protected async Task ShutdownSocket(WebSocket socket)
    {
        try
        {
            if (socket.State == WebSocketState.Open || 
                socket.State == WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, 
                    nameof(WebSocketCloseStatus.NormalClosure), 
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing WebSocket");
        }
        finally
        {
            socket.Dispose();
        }
    }
}
