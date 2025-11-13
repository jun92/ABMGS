using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Sessions;
using ABMGS.ServerV2.SyncnetPlatform.Network.Utils;
using Google.FlatBuffers;
using SyncnetPlatform.Dto;
using System.Net.WebSockets;

namespace ABMGS.ServerV2.SyncnetPlatform.Network.Sessions;

public class GameSessionService : Grain, IGameSessionService
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly FlatBufferPacketRouter _routeTable;
    private readonly ICustomPacketHandler _customPacketHandler;
    private ISystemPacketHandler _systemPacketHandler;

    public GameSessionService(
        ILogger<GameSessionService> logger, 
        IClusterClient clusterClient, 
        ICustomPacketHandler customPacketHandler,
        ISystemPacketHandler systemPacketHandler,
        FlatBufferPacketRouter routeTable)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _routeTable = routeTable;
        _customPacketHandler = customPacketHandler;
        _systemPacketHandler = systemPacketHandler;
    }

    public void Initialize()
    {
        PacketWrapper packet = PacketWrapper.GetRootAsPacketWrapper(BuildDummyPacket());
        _routeTable.BuildParamExtractionFuncs(packet);
        _routeTable.BuildPacketHandlerFunctions(_customPacketHandler);
        
    }
    protected ByteBuffer BuildDummyPacket()
    {
        FlatBufferBuilder flatBufferBuilder = new FlatBufferBuilder(1024);
        Offset<Dummy> dummy = Dummy.CreateDummy(flatBufferBuilder, 0);
        flatBufferBuilder.Finish(dummy.Value);
        return flatBufferBuilder.DataBuffer;
    }

    public async Task StartGameSession(Guid uniquePlayerId, WebSocket SocketObject, CancellationToken abnormalExitToken)
    {
        ArgumentNullException.ThrowIfNull(SocketObject);

        // Let the handlers know who is dealing with.
        _systemPacketHandler.BindPlayer(uniquePlayerId);
        _customPacketHandler.BindPlayer(uniquePlayerId);

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
                _routeTable.Execute(PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(await NBuf.Read())));
            }
        }
        await Task.CompletedTask;
    }
}
