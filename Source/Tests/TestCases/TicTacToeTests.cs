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
        WebSocketReceiveResult result;
        PacketWrapper packetWrapper;

        // Player01 creates a play room.
        (result, packetWrapper) = await SendAndReceive(player01, BuildReqCreatePlayRoomPacket("TestRoom", false, "", 2, null));
        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        ResCreateRoom resCreateRoom = packetWrapper.SystemPacketAsResCreateRoom();
        Assert.Equal(PacketErrorCodes.Success, resCreateRoom.Result);
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(resCreateRoom.RoomId);

        // player02 joins the play room.
        (result, packetWrapper) = await SendAndReceive(player01, BuildReqJoinPlayRoomPacket(roomId));
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        ResJoinRoom resJoinRoom = packetWrapper.SystemPacketAsResJoinRoom();
        Assert.Equal(PacketErrorCodes.Success, resJoinRoom.Result);
        
        // player01 starts a play - put a mark 
        
        


    }
    
}
