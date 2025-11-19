using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace SyncnetPlatform.Network.Sessions;

public class GameSessionService : IGameSessionService
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ISendQueueService _sendQueue;
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
        
        _sendQueue = sendQueue;
        _customPacketHandler = customPacketHandler;
        _systemPacketHandler = systemPacketHandler;
    }

    public async Task StartGameSession(Guid uniquePlayerId, WebSocket SocketObject, CancellationToken abnormalExitToken)
    {
        ArgumentNullException.ThrowIfNull(SocketObject);

        try
        {

            IPacketHandler packetHandlingActor = _clusterClient.GetGrain<IPacketHandler>(uniquePlayerId);
            await _sendQueue.Register(uniquePlayerId, SocketObject);


            //Loop to receive data from the WebSocket
            while (!abnormalExitToken.IsCancellationRequested)
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
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        await Task.CompletedTask;
    }
}
