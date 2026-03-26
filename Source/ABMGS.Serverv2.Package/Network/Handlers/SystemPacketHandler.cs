using Microsoft.Extensions.Logging;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Extensions;
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
        IPlayerActor player = ctx.GetPlayer();
        await player.PingPong(request.Seq);
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request, PacketContext ctx)
    {
        await ctx.SendDataRaw(request.ByteBuffer.ToFullArray());
    }
    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
        string playerName = await player.GetPlayerName();
        await ctx.SendData(new ResUserInfoArgs(ctx.GetPlayerId(), playerName));
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
        await player.UpdatePlayerName(request.PlayerName);
        await ctx.SendData(new ResUpdatePlayerNameArgs(PacketErrorCodes.Success));
    }

    [PacketHandler(typeof(ReqDirectDeliveryData))]
    public async Task HandleReqDirectDeliveryData(ReqDirectDeliveryData request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
        Guid toPlayerId = Guid.NewGuid();

        toPlayerId.FromGuidType(request.ToPlayerId);

        PacketErrorCodes result = await player.SendDirectDeliverData(
            toPlayerId,
            request.Data, 
            request.DataType);

        await ctx.SendData(new ResDirectDeliveryDataArgs(result));
    }

    [PacketHandler(typeof(OnDirectDeliveryData))]
    public async Task HandleOnDirectDeliveryData(OnDirectDeliveryData request, PacketContext ctx)
    {
        await ctx.SendDataRaw(request.ByteBuffer.ToFullArray());
    }

    [PacketHandler(typeof(ReqCreateRoom))]
    public async Task HandleReqCreateroom(ReqCreateRoom request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();

        Guid RoomId = await player.CreateAndJoinPlayRoom(request.Name, request.Private, request.MaxCount, request.Password);
        await ctx.SendData(new ResCreateRoomArgs(PacketErrorCodes.Success, RoomId));
    }
    [PacketHandler(typeof(ReqJoinRoom))]
    public async Task HandleReqJoinRoom(ReqJoinRoom request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
        Guid RoomId = new Guid();
        RoomId.FromGuidType(request.RoomId);
        PacketErrorCodes resultCode = await player.JoinPlayRoom(RoomId);

        await ctx.SendData(new ResJoinRoomArgs(resultCode));
    }

    [PacketHandler(typeof(OnPlayerJoinRoom))]
    public async Task HandleOnPlayerJoinRoom(OnPlayerJoinRoom request, PacketContext ctx)
    {
        await ctx.SendDataRaw(request.ByteBuffer.ToFullArray());
    }

    [PacketHandler(typeof(ReqLeaveRoom))]
    public async Task HandleReqLeavePlayRoom(ReqLeaveRoom request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
        //player

    }
}

