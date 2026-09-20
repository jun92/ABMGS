using Silo.Player;
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
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        ResUserInfo player1Info = packetWrapper.SystemPacketAsResUserInfo();
        player1Id.FromGuidType(player1Info.Id);
        Assert.NotEqual(Guid.Empty, player1Id);
        
        // Get player2 Id
        (result, packetWrapper) = await SendAndReceive(player02, BuildReqUserInfoPacket());
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        ResUserInfo player2Info = packetWrapper.SystemPacketAsResUserInfo();
        player2Id.FromGuidType(player2Info.Id);
        Assert.NotEqual(Guid.Empty, player2Id);

        // Player01 creates a play room.
        (result, packetWrapper) = await SendAndReceive(player01, BuildReqCreatePlayRoomPacket("TestRoom", false, "", 2, null));
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        ResCreateRoom resCreateRoom = packetWrapper.SystemPacketAsResCreateRoom();
        Assert.Equal(PacketErrorCodes.Success, resCreateRoom.Result);
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(resCreateRoom.RoomId);

        // player02 joins the play room.
        (result, packetWrapper) = await SendAndReceive(player02, BuildReqJoinPlayRoomPacket(roomId));
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        ResJoinRoom resJoinRoom = packetWrapper.SystemPacketAsResJoinRoom();
        Assert.Equal(PacketErrorCodes.Success, resJoinRoom.Result);
        
        // Player3 can't join
        (result, packetWrapper) = await SendAndReceive(playerCannotJoin, BuildReqJoinPlayRoomPacket(roomId));
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        ResJoinRoom resJoinRoomFail = packetWrapper.SystemPacketAsResJoinRoom();
        Assert.Equal(PacketErrorCodes.RoomFull, resJoinRoomFail.Result);

        // player01 gets the nofitication of player02 in.
        (result, packetWrapper) = await ReceiveAsync(player01);
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.OnPlayerJoinRoom, packetWrapper.SystemPacketType);
        OnPlayerJoinRoom onPlayerJoinRoom = packetWrapper.SystemPacketAsOnPlayerJoinRoom();

        
        // Player1 sends ready packet
        byte[] setRedayParameters01 = BuildTGameReqActionSetReady(player1Id, true);
        byte[] reqPlayerActionToPlayRoom01 = BuildReqPlayerActionToPlayRoom(roomId, Command.Ready, setRedayParameters01);

        (result, packetWrapper) = await SendAndReceive(player01, reqPlayerActionToPlayRoom01);
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.OnPlayRoomStateUpdate, packetWrapper.SystemPacketType);
        OnPlayRoomStateUpdate onPlayRoomStateUpdate01 = packetWrapper.SystemPacketAsOnPlayRoomStateUpdate();
        byte[] serailizedPlayRoomState = onPlayRoomStateUpdate01.GetUpdatedRoomStateArray();

        TttGamePlayRoomState playRoomState = new TttGamePlayRoomState();
        playRoomState.Deserialize(serailizedPlayRoomState);

        // bool isPlayerReady = false;
        
        Assert.True(playRoomState.PlayerReadyState.TryGetValue(player1Id, out bool isPlayerReady));
        Assert.True(isPlayerReady);
        
        Assert.True(playRoomState.PlayerReadyState.TryGetValue(player2Id, out isPlayerReady));
        Assert.False(isPlayerReady);

        // check the same playroom states are delivered to player02.
        (result, packetWrapper) = await ReceiveAsync(player02);
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.OnPlayRoomStateUpdate, packetWrapper.SystemPacketType);
        OnPlayRoomStateUpdate onPlayerRoomStateUpdate02 = packetWrapper.SystemPacketAsOnPlayRoomStateUpdate();
        
        playRoomState.Deserialize(onPlayerRoomStateUpdate02.GetUpdatedRoomStateArray());
        Assert.True(playRoomState.PlayerReadyState.TryGetValue(player1Id, out  isPlayerReady));
        Assert.True(isPlayerReady);
        
        
        // player02 sends ready packet as well.
        byte[] setRedayParameters02 = BuildTGameReqActionSetReady(player2Id, true);
        byte[] reqPlayerActionToPlayRoom02 = BuildReqPlayerActionToPlayRoom(roomId, Command.Ready, setRedayParameters02);
        (result, packetWrapper) = await SendAndReceive(player02, reqPlayerActionToPlayRoom02);
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.OnPlayRoomStateUpdate, packetWrapper.SystemPacketType);
        OnPlayRoomStateUpdate onPlayRoomStateUpdate02 = packetWrapper.SystemPacketAsOnPlayRoomStateUpdate();
        serailizedPlayRoomState = onPlayRoomStateUpdate02.GetUpdatedRoomStateArray();
        playRoomState.Deserialize(serailizedPlayRoomState);
        
        Assert.True(playRoomState.PlayerReadyState.TryGetValue(player1Id, out isPlayerReady));
        Assert.True(isPlayerReady);
        
        Assert.True(playRoomState.PlayerReadyState.TryGetValue(player2Id, out isPlayerReady));
        Assert.True(isPlayerReady);

        await PutMarkerOnBoardTest(player01, player1Id, 0, 0, playRoomState);
        await PutMarkerOnBoardTest(player02, player2Id, 1, 1, playRoomState);

        
    }

    private async Task PutMarkerOnBoardTest(ClientWebSocket playerConn, Guid playerId, int x, int y, TttGamePlayRoomState tttGamePlayRoomState)
    {
        byte[] reqPlayerActionToPlayRoom = BuildTGameReqActionPutMarker(playerId, x, y);
        (WebSocketReceiveResult result, PacketWrapper packetWrapper) = await SendAndReceive(playerConn, reqPlayerActionToPlayRoom);
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.OnPlayRoomStateUpdate, packetWrapper.SystemPacketType);
        OnPlayRoomStateUpdate onPlayRoomStateUpdate = packetWrapper.SystemPacketAsOnPlayRoomStateUpdate();
        
        tttGamePlayRoomState.Deserialize(onPlayRoomStateUpdate.GetUpdatedRoomStateArray());
        Assert.Equal(tttGamePlayRoomState.BoardState[x, y].PlayerId, playerId);
    }
    
}
