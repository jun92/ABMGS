using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;

namespace SyncnetPlatform.Network.Handlers;

public class SystemPacketHandler : PacketHandlerBase, ISystemPacketHandler
{
    protected Guid _playerId;
    public SystemPacketHandler(ILogger<SystemPacketHandler> logger, IClusterClient clusterClient) : base(logger, clusterClient)
    {
    }

    public async Task BindPlayer(Guid playerId, WebSocket webSocket)
    {
        _playerId = playerId;
        IPlayerActor player = GetPlayer(_playerId);
        await player.Initialize(webSocket);
    }

    [PacketHandler(typeof(Dummy))]
    public async Task HandleDummpy(Dummy dummpy)
    {
        _logger.LogError("Dummy packet received. Are you dummy?");
    }

    
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        _logger.LogInformation($"HandlePing, Seq is {request.Seq}");
        IPlayerActor player = GetPlayer(_playerId);
        await player.Echo(request.Seq);
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request)
    {
        _logger.LogError("This should not be called.");
    }

}
