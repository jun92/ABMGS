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
        string playerName = await player.GetPlayerName();
        await ctx.SendData(
            ctx.GetPlayerId(),
            SyncnetPacketBuilder.Build(new ResUserInfoArgs(ctx.GetPlayerId(), playerName))
            );
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
        await player.UpdatePlayerName(request.PlayerName);

        await ctx.SendData(
            ctx.GetPlayerId(), 
            SyncnetPacketBuilder.Build(new ResUpdatePlayerNameArgs(0, "Success"))
        );
    }

    
}

