using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Exceptions;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Channels;
using SyncnetPlatform.Extensions;
using System.Diagnostics;
using SyncnetPlatform.Utils.Telemetry;

namespace SyncnetPlatform.Network.Sessions;

public class GameSessionService : IGameSessionService, ISendDataObserver
{
    protected readonly struct PendingSendPacket
    {
        public byte[] Data { get; }
        public ActivityContext ParentContext { get; }

        public PendingSendPacket(byte[] data, ActivityContext parentContext)
        {
            Data = data;
            ParentContext = parentContext;
        }
    }
    private readonly SyncnetMetricsService _syncnetMetricsService;
    private readonly ILogger<GameSessionService> _logger;
    private readonly IClusterClient _clusterClient;

    private readonly Channel<PendingSendPacket> _sendingQueueChannel;
    private ISendDataObserver? _sendDataObserver;
    private Task? _sendLoopTask;

    public GameSessionService(
        ILogger<GameSessionService> logger, 
        IClusterClient clusterClient,
        SyncnetMetricsService syncnetMetricsService)
    {
        _logger = logger;
        _clusterClient = clusterClient;

        _sendingQueueChannel = Channel.CreateBounded<PendingSendPacket>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            Capacity = 100 // TODO: Should go to config something later.
        });
        _sendDataObserver = null;
        _syncnetMetricsService = syncnetMetricsService;
    }

    public async Task SendDataAsync(byte[] data)
    {
        ActivityContext parentContext = default;
        if( RequestContext.Get("traceparent") is string traceparent)
        {
            parentContext = ActivityContext.Parse(traceparent, null );
        }
        else
        {
            parentContext = Activity.Current?.Context ?? default;
        }
        //var parentContext = Activity.Current?.Context ?? default;
        await _sendingQueueChannel.Writer.WriteAsync(new PendingSendPacket(data, parentContext));
    }

    protected async Task SendDataLoop(
        WebSocket socket, 
        Channel<PendingSendPacket> channel, 
        CancellationToken sendLoopExitToken)
    {
        try
        {
            await foreach (var pending in channel.Reader.ReadAllAsync(sendLoopExitToken))
            {
                if (socket.State != WebSocketState.Open) break;

                using var sendActivity = SyncnetTelemetry.Trace.StartActivity(
                    "SendResponse", 
                    ActivityKind.Internal, 
                    parentContext: pending.ParentContext);

                await socket.SendAsync(
                    new ArraySegment<byte>(pending.Data),
                    WebSocketMessageType.Binary,
                    true,
                    sendLoopExitToken);
            }
        }
        catch(ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Socket disposed.");
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Socket disabled.");
        }
        catch(WebSocketException ex)
        {
            _logger.LogError(ex, "Socket operation error in SendAsync");
        }
        catch(OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Sending Loop was canceled. Shutting down.");
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
        _sendDataObserver = _clusterClient.CreateObjectReference<ISendDataObserver>(this);
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
        IPlayerActor playerActor = _clusterClient.GetGrain<IPlayerActor>(playerId);
        await playerActor.SetOnline(true);

        byte[] receiveBuffer = new byte[4096];
        using var ms = new MemoryStream();
        bool exitLoop = false;

        _syncnetMetricsService.AddConnection();

        while (!mainLoopExitToken.IsCancellationRequested && !exitLoop)
        {
            ms.SetLength(0);
            while (true)
            {
                ValueWebSocketReceiveResult result;
                try
                {
                    result = await SocketObject.ReceiveAsync(new Memory<byte>(receiveBuffer), mainLoopExitToken);
                }
                catch(OperationCanceledException)
                {
                    // Exit with nothing wrong
                    exitLoop = true;
                    break;
                }
                catch(WebSocketException ex)
                {
                    // Abnormal socket exception and closure.
                    _logger.LogWarning(ex, "Socket closed abnormally.");
                    SocketObject.Abort();
                    exitLoop = true;
                    break;
                }
                catch(FlatBufferPacketBuildException e)
                {
                    _logger.LogCritical(e, "FlatBuffer Exception");
                    exitLoop = true;
                    break;
                }

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
                    exitLoop = true;
                    break;
                }

                ms.Write(receiveBuffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    await playerActor.PushRecievedData(ms.ToArray());
                    break;
                }
            }
        }
        await playerActor.SetOnline(false);
        _syncnetMetricsService.RemoveConnection();

    }
    public async Task StartGameSession(Guid uniquePlayerId, WebSocket SocketObject)
    {
        ArgumentNullException.ThrowIfNull(SocketObject);
        uniquePlayerId.ThrowIfInvalidGuid();
        
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
