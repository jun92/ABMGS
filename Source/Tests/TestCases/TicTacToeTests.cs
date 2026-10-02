using Silo.Player;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;

namespace SyncnetPlatform.Tests;

public partial class ABMGS_TestMain
{
    private async Task<Guid> CreatePlayRoom(ClientWebSocket creator)
    {
        var (result, packetWrapper) = await SendAndReceive(creator, BuildReqCreatePlayRoomPacket("TestRoom", false, "", 2, null));
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        ResCreateRoom resCreateRoom = packetWrapper.SystemPacketAsResCreateRoom();
        Assert.Equal(PacketErrorCodes.Success, resCreateRoom.Result);
        Guid roomId = resCreateRoom.RoomId.ToGuid();
        Assert.NotEqual(Guid.Empty, roomId);
        return roomId;
    }

    private async Task JoinPlayRoom(ClientWebSocket joiner, Guid roomId)
    {
        var (result, packetWrapper) = await SendAndReceive(joiner, BuildReqJoinPlayRoomPacket(roomId));
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        ResJoinRoom resJoinRoom = packetWrapper.SystemPacketAsResJoinRoom();
        Assert.Equal(PacketErrorCodes.Success, resJoinRoom.Result);
    }
    
    private async Task CannotJoinPlayRoom(ClientWebSocket failer, Guid roomId)
    {
        var (result, packetWrapper) = await SendAndReceive(failer, BuildReqJoinPlayRoomPacket(roomId));
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResJoinRoom, packetWrapper.SystemPacketType);
        ResJoinRoom resJoinRoom = packetWrapper.SystemPacketAsResJoinRoom();
        Assert.Equal(PacketErrorCodes.RoomFull, resJoinRoom.Result);
    }
    
    [Fact]
    public async Task TicTacToeGamePlayTest()
    {
        var player01 = await CreateAuthoredWebSocket();
        var player02 = await CreateAuthoredWebSocket();
        var playerCannotJoin = await CreateAuthoredWebSocket();

        WebSocketReceiveResult result;
        PacketWrapper packetWrapper;
        
        // Get Player's Guids
        Guid player1Id = await GetPlayerIdAndAssert(player01);
        Guid player2Id = await GetPlayerIdAndAssert(player02);

        // Player01 creates a play room.
        Guid roomId = await CreatePlayRoom(player01);
        // player02 joins the play room.
        await JoinPlayRoom(player02, roomId);
        // Player3 can't join
        await CannotJoinPlayRoom(playerCannotJoin, roomId);

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

        // await PutMarkerOnBoardTest(player01, player1Id, 0, 0, playRoomState);
        // await PutMarkerOnBoardTest(player02, player2Id, 1, 1, playRoomState);

        await CloseAuthoredWebSocket(player01);
        await CloseAuthoredWebSocket(player02);
        await CloseAuthoredWebSocket(playerCannotJoin);

    }
    
    private async Task<Guid> GetPlayerIdAndAssert(ClientWebSocket client)
    {
        var (result, packetWrapper) = await SendAndReceive(client, BuildReqUserInfoPacket());
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.ResUserInfo, packetWrapper.SystemPacketType);
        ResUserInfo playerInfo = packetWrapper.SystemPacketAsResUserInfo();
        Guid playerId = playerInfo.Id.ToGuid();
        Assert.NotEqual(Guid.Empty, playerId);
        return playerId;
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
