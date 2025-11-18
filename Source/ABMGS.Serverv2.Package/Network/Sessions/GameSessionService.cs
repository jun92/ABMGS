using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;


namespace SyncnetPlatform.Network.Sessions;

public class GameSessionService : Grain, IGameSessionService
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly FlatBufferPacketRouter _routeTable;
    private readonly ICustomPacketHandler _customPacketHandler;
    private readonly SystemPacketHandler _systemPacketHandler;

    public GameSessionService(
        ILogger<GameSessionService> logger, 
        IClusterClient clusterClient, 
        ICustomPacketHandler customPacketHandler,
        SystemPacketHandler systemPacketHandler,
        FlatBufferPacketRouter routeTable)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _routeTable = routeTable;
        _customPacketHandler = customPacketHandler;
        _systemPacketHandler = systemPacketHandler;
    }

    public async Task StartGameSession(Guid uniquePlayerId, WebSocket SocketObject, CancellationToken abnormalExitToken)
    {
        ArgumentNullException.ThrowIfNull(SocketObject);

        // Let the handlers know who is dealing with.
        //await _systemPacketHandler.BindPlayer(uniquePlayerId, SocketObject);
        //_customPacketHandler.BindPlayer(uniquePlayerId);
        IPacketHandler packetHandlingActor = _clusterClient.GetGrain<IPacketHandler>(uniquePlayerId);

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
