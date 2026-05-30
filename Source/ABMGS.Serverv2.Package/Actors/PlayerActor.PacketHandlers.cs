using Microsoft.Extensions.Logging;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Network.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncnetPlatform.Actors;

public partial class PlayerActor
{
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request, PacketContext ctx)
    {
        await PingPong(request.Seq);
    }

    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request, PacketContext ctx)
    {
        string playerName = await GetPlayerName();
        await ctx.SendData(new ResUserInfoArgs(ctx.GetPlayerId(), playerName));
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request, PacketContext ctx)
    {
        await UpdatePlayerName(request.PlayerName);
        await ctx.SendData(new ResUpdatePlayerNameArgs(PacketErrorCodes.Success));
    }

    [PacketHandler(typeof(ReqDirectDeliveryData))]
    public async Task HandleReqDirectDeliveryData(ReqDirectDeliveryData request, PacketContext ctx)
    {
        Guid toPlayerId = default;
        toPlayerId.FromGuidType(request.ToPlayerId);

        PacketErrorCodes result = await SendDirectDeliverData(
            toPlayerId,
            request.Data, 
            request.DataType);

        await ctx.SendData(new ResDirectDeliveryDataArgs(result));
    }

    [PacketHandler(typeof(ReqCreateRoom))]
    public async Task HandleReqCreateroom(ReqCreateRoom request, PacketContext ctx)
    {
        Guid RoomId = await CreateAndJoinPlayRoom(request.Name, request.Private, request.MaxCount, request.Password);
        await ctx.SendData(new ResCreateRoomArgs(PacketErrorCodes.Success, RoomId));
    }

    [PacketHandler(typeof(ReqJoinRoom))]
    public async Task HandleReqJoinRoom(ReqJoinRoom request, PacketContext ctx)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        PacketErrorCodes resultCode = await JoinPlayRoom(RoomId);

        await ctx.SendData(new ResJoinRoomArgs(resultCode));
    }

    [PacketHandler(typeof(ReqPlayerListInRoom))]
    public async Task HandleReqPlayerListInRoom(ReqPlayerListInRoom request, PacketContext ctx)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        List<PlayRoomMember> Players = await GetPlayerListInPlayRoom(RoomId);

        await ctx.SendData(
            new ResPlayerListInRoomArgs(
                RoomId, 
                [.. Players.Select(s => new PlayerInfoInRoomArgs(s.PlayerId, s.PlayerName))]
               ));
    }

    [PacketHandler(typeof(ReqLeaveRoom))]
    public async Task HandleReqLeavePlayRoom(ReqLeaveRoom request, PacketContext ctx)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        PacketErrorCodes result = await LeavePlayRoom(RoomId);

        await ctx.SendData<ResLeaveRoomArgs>(
            new ResLeaveRoomArgs(result)
            );
    }
}
