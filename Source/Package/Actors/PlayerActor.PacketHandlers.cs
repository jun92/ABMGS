using Microsoft.Extensions.Logging;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Network.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PacketBuilder = SyncnetPlatform.Network.Utils.SyncnetPacketBuilder;

namespace SyncnetPlatform.Actors;

public partial class PlayerActor
{
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        await PingPong(request.Seq);
    }

    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request)
    {
        string playerName = await GetPlayerName();
        await _sendDataGrain.Send(PacketBuilder.Build<ResUserInfoArgs>(new ResUserInfoArgs(PlayerId, playerName)));
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request)
    {
        await UpdatePlayerName(request.PlayerName);
        await _sendDataGrain.Send(PacketBuilder.Build<ResUpdatePlayerNameArgs>(new ResUpdatePlayerNameArgs(PacketErrorCodes.Success)));
    }

    [PacketHandler(typeof(ReqDirectDeliveryData))]
    public async Task HandleReqDirectDeliveryData(ReqDirectDeliveryData request)
    {
        Guid toPlayerId = default;
        toPlayerId.FromGuidType(request.ToPlayerId);

        PacketErrorCodes result = await SendDirectDeliverData(
            toPlayerId,
            request.Data, 
            request.DataType);

        await _sendDataGrain.Send(PacketBuilder.Build<ResDirectDeliveryDataArgs>(new ResDirectDeliveryDataArgs(result)));
    }

    [PacketHandler(typeof(ReqCreateRoom))]
    public async Task HandleReqCreateroom(ReqCreateRoom request)
    {
        Guid RoomId = await CreateAndJoinPlayRoom(request.Name, request.Private, request.MaxCount, request.Password);
        await _sendDataGrain.Send(PacketBuilder.Build<ResCreateRoomArgs>(new ResCreateRoomArgs(PacketErrorCodes.Success, RoomId)));
    }

    [PacketHandler(typeof(ReqJoinRoom))]
    public async Task HandleReqJoinRoom(ReqJoinRoom request)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        PacketErrorCodes resultCode = await JoinPlayRoom(RoomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResJoinRoomArgs>(new ResJoinRoomArgs(resultCode)));
    }

    [PacketHandler(typeof(ReqPlayerListInRoom))]
    public async Task HandleReqPlayerListInRoom(ReqPlayerListInRoom request)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        List<PlayRoomMember> Players = await GetPlayerListInPlayRoom(RoomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResPlayerListInRoomArgs>(
            new ResPlayerListInRoomArgs(
                RoomId, 
                [.. Players.Select(s => new PlayerInfoInRoomArgs(s.PlayerId, s.PlayerName))]
               )));
    }

    [PacketHandler(typeof(ReqLeaveRoom))]
    public async Task HandleReqLeavePlayRoom(ReqLeaveRoom request)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        PacketErrorCodes result = await LeavePlayRoom(RoomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResLeaveRoomArgs>(
            new ResLeaveRoomArgs(result)
            ));
    }
}
