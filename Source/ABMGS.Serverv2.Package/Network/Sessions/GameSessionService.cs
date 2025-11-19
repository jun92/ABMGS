using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

using SyncnetPlatform.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;

namespace SyncnetPlatform.Network.Sessions;

public class GameSessionService : IGameSessionService
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ISendQueueService _sendQueueService;
    private readonly ICustomPacketHandler _customPacketHandler;
    private readonly SystemPacketHandler _systemPacketHandler;

    public GameSessionService(
        ILogger<GameSessionService> logger, 
        IClusterClient clusterClient,
        ISendQueueService sendQueue,
        ICustomPacketHandler customPacketHandler,
        SystemPacketHandler systemPacketHandler)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        
        _sendQueueService = sendQueue;
        _customPacketHandler = customPacketHandler;
        _systemPacketHandler = systemPacketHandler;
    }

    public async Task StartGameSession(Guid uniquePlayerId, WebSocket SocketObject)
    {
        ArgumentNullException.ThrowIfNull(SocketObject);
        CancellationTokenSource LoopEndToken = new CancellationTokenSource();
        CancellationTokenSource SendExceptionToken = new CancellationTokenSource();

        try
        {
            IPacketHandler packetHandlingActor = _clusterClient.GetGrain<IPacketHandler>(uniquePlayerId);
            await _sendQueueService.Register(uniquePlayerId, SocketObject, SendExceptionToken);

            while (!SendExceptionToken.IsCancellationRequested && !LoopEndToken.IsCancellationRequested)
            {
                using NetworkBuffer NBuf = new(4096);
                while (true)
                {
                    ValueWebSocketReceiveResult result;
                    result = await SocketObject.ReceiveAsync(NBuf.GetReceiveBuffer(), SendExceptionToken.Token);
                    NBuf.AddBuffer(result.Count);

                    if (result.MessageType == WebSocketMessageType.Close || result.Count == 0)
                    {
                        await SocketObject.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Socket Closed",
                            CancellationToken.None);
                        LoopEndToken.Cancel();
                        break;
                    }

                    if (result.EndOfMessage == true)
                    {
                        await NBuf.FinishReceived();
                        await packetHandlingActor.PushRecievedData(await NBuf.Read());
                        break;
                    }
                }
            }
        }
        catch(Exception e) when (
                        e is WebSocketException ||
                        e is OperationCanceledException ||
                        e is ObjectDisposedException)
        {
        }
        finally
        {
            //Cleanup 
            await _sendQueueService.Unregister(uniquePlayerId);

            SocketObject.Dispose();
            LoopEndToken.Dispose();
            SendExceptionToken.Dispose();
        }
    }
}
