using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Xunit;

namespace SyncnetPlatform.Tests;
public partial class ABMGS_TestMain : IAsyncLifetime
{
    [Fact]
    public async Task PlayroomCreationAndDestructionTest()
    {
        var wsClient = await CreateAuthoredWebSocket();
        // Enter
        var (result, packetWrapper) = await SendAndReceive(wsClient, BuildReqCreatePlayRoomPacket("CreateRoomTestTitle"));
        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResCreateRoom().Result);
        
        Guid roomId = packetWrapper.SystemPacketAsResCreateRoom().RoomId.ToGuid();

        // Leave
        (result, packetWrapper) = await SendAndReceive(wsClient, BuildReqLeavelPlayRoomPacket(roomId));

        Assert.Equal(SystemPacket.ResLeaveRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResLeaveRoom().Result);

        // try to join the room already closed.
        (result, packetWrapper) = await SendAndReceive(wsClient, BuildReqJoinPlayRoomPacket(roomId));

        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.RoomNotFound, packetWrapper.SystemPacketAsResJoinRoom().Result);
    }

    [Fact]
    public async Task PlayroomCreationAndDestructionTestWithMetadata()
    {

    }


    [Fact]
    public async Task PlayroomNotExistingFailTest()
    {
        var wsClient = await CreateAuthoredWebSocket();

        Guid roomId = Guid.NewGuid();
        var (result, packetWrapper) = await SendAndReceive(wsClient, BuildReqJoinPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.RoomNotFound, packetWrapper.SystemPacketAsResJoinRoom().Result);
    }

    [Fact]
    public async Task TwoPlayersJoinPlayRoomTest()
    {
        var wsClientOwner = await CreateAuthoredWebSocket();
        var wsClientJoiner = await CreateAuthoredWebSocket();

        //byte[] dataToSend;
        WebSocketReceiveResult result;
        PacketWrapper packetWrapper;


        // Get player Id of owner/joiner.
        byte[] ReqUserInfoPacket = BuildReqUserInfoPacket();
        

        // Owner info.
        (result, packetWrapper) = await SendAndReceive(wsClientOwner, ReqUserInfoPacket);
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        Guid ownerPlayerId = packetWrapper.SystemPacketAsResUserInfo().Id.ToGuid();

        // Joiner info.
        (result, packetWrapper) = await SendAndReceive(wsClientJoiner, ReqUserInfoPacket);
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        Guid joinerPlayerId = packetWrapper.SystemPacketAsResUserInfo().Id.ToGuid();

        // Owner creates a room.
        (result, packetWrapper) = await SendAndReceive(wsClientOwner, BuildReqCreatePlayRoomPacket("CreateRoomTestTitle", false, "", 5));

        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResCreateRoom().Result);
        Guid roomId = packetWrapper.SystemPacketAsResCreateRoom().RoomId.ToGuid();

        // Joiner trys to join.
        (result, packetWrapper) = await SendAndReceive(wsClientJoiner, BuildReqJoinPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResJoinRoom().Result);

        // Owner get the nofification of new joiner.
        (result, packetWrapper) = await ReceiveAsync(wsClientOwner);
        Assert.Equal(SystemPacket.OnPlayerJoinRoom, packetWrapper.SystemPacketType);

        Guid recvRoomId = packetWrapper.SystemPacketAsOnPlayerJoinRoom().RoomId.ToGuid();
        Guid joinedPlayerId = packetWrapper.SystemPacketAsOnPlayerJoinRoom().JoinerId.ToGuid();

        Assert.Equal(roomId, recvRoomId);
        Assert.Equal(joinerPlayerId, joinedPlayerId);

        // Getting plaer list 

        (result, packetWrapper) = await SendAndReceive(wsClientJoiner, BuildReqPlayerListInRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResPlayerListInRoom, packetWrapper.SystemPacketType);
        ResPlayerListInRoom playerList = packetWrapper.SystemPacketAsResPlayerListInRoom();
        Assert.Equal(2, playerList.MembersLength);


        (result, packetWrapper) = await SendAndReceive(wsClientOwner, BuildReqLeavelPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResLeaveRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResLeaveRoom().Result);

        (result, packetWrapper) = await ReceiveAsync(wsClientJoiner);
        Assert.Equal(SystemPacket.OnPlayerLeaveRoom, packetWrapper.SystemPacketType);
        Guid notifiedLeaverPlayerId = packetWrapper.SystemPacketAsOnPlayerLeaveRoom().PlayerId.ToGuid();
        Assert.Equal(ownerPlayerId, notifiedLeaverPlayerId);

        (result, packetWrapper) = await SendAndReceive(wsClientJoiner, BuildReqLeavelPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResLeaveRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResLeaveRoom().Result);

    }

}
