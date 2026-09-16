using SyncnetPlatform.Extensions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;

namespace SyncnetPlatform.Tests;

public partial class ABMGS_TestMain
{
    [Fact]
    public async Task TicTacToeGamePlayTest()
    {
        var player01 = await CreateAuthoredWebSocket();
        var player02 = await CreateAuthoredWebSocket();
        var playerCannotJoin = await CreateAuthoredWebSocket();

        Guid player1Id = Guid.Empty;
        Guid player2Id = Guid.Empty;
        WebSocketReceiveResult result;
        PacketWrapper packetWrapper;

        // Get player1 Id
        (result, packetWrapper) = await SendAndReceive(player01, BuildReqUserInfoPacket());
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        ResUserInfo player1Info = packetWrapper.SystemPacketAsResUserInfo();
        player1Id.FromGuidType(player1Info.Id);
        Assert.NotEqual(Guid.Empty, player1Id);
        
        // Get player2 Id
        (result, packetWrapper) = await SendAndReceive(player02, BuildReqUserInfoPacket());
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        ResUserInfo player2Info = packetWrapper.SystemPacketAsResUserInfo();
        player2Id.FromGuidType(player2Info.Id);
        Assert.NotEqual(Guid.Empty, player2Id);

        // Player01 creates a play room.
        (result, packetWrapper) = await SendAndReceive(player01, BuildReqCreatePlayRoomPacket("TestRoom", false, "", 2, null));
        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        ResCreateRoom resCreateRoom = packetWrapper.SystemPacketAsResCreateRoom();
        Assert.Equal(PacketErrorCodes.Success, resCreateRoom.Result);
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(resCreateRoom.RoomId);

        // player02 joins the play room.
        (result, packetWrapper) = await SendAndReceive(player02, BuildReqJoinPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        ResJoinRoom resJoinRoom = packetWrapper.SystemPacketAsResJoinRoom();
        Assert.Equal(PacketErrorCodes.Success, resJoinRoom.Result);
        
        // Player3 can't join
        (result, packetWrapper) = await SendAndReceive(playerCannotJoin, BuildReqJoinPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        ResJoinRoom resJoinRoomFail = packetWrapper.SystemPacketAsResJoinRoom();
        Assert.Equal(PacketErrorCodes.RoomFull, resJoinRoomFail.Result);


        (result, packetWrapper) = await ReceiveAsync(player01);
        Assert.Equal(SystemPacket.OnPlayerJoinRoom, packetWrapper.SystemPacketType);
        OnPlayerJoinRoom onPlayerJoinRoom = packetWrapper.SystemPacketAsOnPlayerJoinRoom();



        // player01 starts a play - put a mark 
        // byte[] reqPlayerActionToPlayRoom = BuildReqPlayerActionToPlayRoom(roomId, "PutMarker", []);
        //
        // (result, packetWrapper) = await SendAndReceive(player01, reqPlayerActionToPlayRoom);






    }
    
}
