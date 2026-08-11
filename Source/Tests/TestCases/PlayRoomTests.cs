using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;

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
        
        Guid roomId = default;
        roomId.FromGuidType(packetWrapper.SystemPacketAsResCreateRoom().RoomId);

        // Leave
        (result, packetWrapper) = await SendAndReceive(wsClient, BuildReqLeavelPlayRoomPacket(roomId));

        Assert.Equal(SystemPacket.OnPlayerLeaveRoom, packetWrapper.SystemPacketType);

        (result, packetWrapper) = await ReceiveAsync(wsClient);
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
        Guid OwnerPlayerId = default;
        OwnerPlayerId.FromGuidType(packetWrapper.SystemPacketAsResUserInfo().Id);

        // Joiner info.
        (result, packetWrapper) = await SendAndReceive(wsClientJoiner, ReqUserInfoPacket);
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        Guid JoinerPlayerId = default;
        JoinerPlayerId.FromGuidType(packetWrapper.SystemPacketAsResUserInfo().Id);

        // Owner creates a room.
        (result, packetWrapper) = await SendAndReceive(wsClientOwner, BuildReqCreatePlayRoomPacket("CreateRoomTestTitle", false, "", 5));
        //(result, packetWrapper) = await SendAndReceive(wsClientOwner, BuildReqCreatePlayRoomPacket("CreateRoomTestTitle"));

        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResCreateRoom().Result);
        Guid roomId = default;
        roomId.FromGuidType(packetWrapper.SystemPacketAsResCreateRoom().RoomId);

        // Joiner trys to join.
        (result, packetWrapper) = await SendAndReceive(wsClientJoiner, BuildReqJoinPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResJoinRoom().Result);

        // Owner get the nofification of new joiner.
        (result, packetWrapper) = await ReceiveAsync(wsClientOwner);
        Assert.Equal(SystemPacket.OnPlayerJoinRoom, packetWrapper.SystemPacketType);

        Guid RecvRoomId = default;
        Guid JoinedPlayerId = default;

        RecvRoomId.FromGuidType(packetWrapper.SystemPacketAsOnPlayerJoinRoom().RoomId);
        JoinedPlayerId.FromGuidType(packetWrapper.SystemPacketAsOnPlayerJoinRoom().JoinerId);

        Assert.Equal(roomId, RecvRoomId);
        Assert.Equal(JoinerPlayerId, JoinedPlayerId);

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
        Guid notifiedLeaverPlayerId = new Guid();
        notifiedLeaverPlayerId.FromGuidType(packetWrapper.SystemPacketAsOnPlayerLeaveRoom().PlayerId);
        Assert.Equal(OwnerPlayerId, notifiedLeaverPlayerId);

        (result, packetWrapper) = await SendAndReceive(wsClientJoiner, BuildReqLeavelPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResLeaveRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResLeaveRoom().Result);

    }

}
