using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Handlers;

public class SystemPacketHandler : ISystemPacketHandler
{
    private readonly ILogger<SystemPacketHandler> _logger;
    public SystemPacketHandler(ILogger<SystemPacketHandler> logger)
    {
        _logger = logger;
    }

    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request, PacketContext ctx)
    {
        _logger.LogInformation($"HandlePing, Seq is {request.Seq}");
        
        await ctx.SendData(
            ctx.GetPlayerId(),
            SyncnetPacketBuilder.Build(new PongArgs(request.Seq + 1)));
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request, PacketContext ctx)
    {
        _logger.LogError("This should not be called.");
    }
    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
    }

    [PacketHandler(typeof(ReqCreateNewUser))]
    public async Task HandleReqCreateNewUser(ReqCreateNewUser request, PacketContext ctx)
    {
        var player = ctx.GetPlayer();

    }
}

