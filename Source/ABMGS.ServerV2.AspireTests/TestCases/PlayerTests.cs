using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace ABMGS.ServerV2.AspireTest;

public partial class ABMGS_TestMain : IAsyncLifetime
{
    [Fact]
    public async Task PlayerNameUpdateText()
    {
        var wsClient = await CreateAuthoredWebSocket();

        string RandomPlayerName = "Guest" + CreateRandomString(6);
        var dataToSend = BuildUpdatePlayerNamePacket(RandomPlayerName);

        await SendDataAsync(wsClient, dataToSend);
        var (receiveBuffer, result) = await ReceiveAsync(wsClient);

        _output.WriteLine($"Count: {result.Count}");
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);

        PacketWrapper packetWrapper = AsPacketWrapper(receiveBuffer, result.Count);
        Assert.Equal(SystemPacket.ResUpdatePlayerName, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResUpdatePlayerName().Result);

        dataToSend = BuildReqUserInfoPacket();
        await SendDataAsync(wsClient, dataToSend);
        (receiveBuffer, result) = await ReceiveAsync(wsClient);

        Assert.True(result.EndOfMessage);
        packetWrapper = AsPacketWrapper(receiveBuffer, result.Count);

        Assert.Equal(RandomPlayerName, packetWrapper.SystemPacketAsResUserInfo().PlayerName);

        await CloseAuthoredWebSocket(wsClient);
    }
    [Fact]
    public async Task DirectDeliveryDataTest()
    {
        var wsClient1 = await CreateAuthoredWebSocket();
        var wsClient2 = await CreateAuthoredWebSocket();

        var getUserInfoPacket = BuildReqUserInfoPacket();

        //Get Client1 User info
        await SendDataAsync(wsClient1, getUserInfoPacket);
        var (receiveBuffer, result) = await ReceiveAsync(wsClient1);
        ResUserInfo UserInfoClient1 = AsPacketWrapper(receiveBuffer, result.Count).SystemPacketAsResUserInfo();
        Guid player1Id = new();
        player1Id.FromGuidType(UserInfoClient1.PlayerId);
        Assert.NotEqual(Guid.Empty, player1Id);

        //Get Client2 User info
        await SendDataAsync(wsClient2, getUserInfoPacket);
        (receiveBuffer, result) = await ReceiveAsync(wsClient2);
        ResUserInfo UserInfoClient2 = AsPacketWrapper(receiveBuffer, result.Count).SystemPacketAsResUserInfo();
        Guid player2Id = new();
        player2Id.FromGuidType(UserInfoClient2.PlayerId);
        Assert.NotEqual(Guid.Empty, player2Id);

        // Two different accounts.
        Assert.NotEqual(player1Id, player2Id);


        string messageToSend = "Hello Friend";
        var ReqDirectMessage = BuildReqDirectDeliveryData(player2Id, messageToSend, DirectDeliveryDataType.Whipher);
        await SendDataAsync(wsClient1, ReqDirectMessage);

        (receiveBuffer, result) = await ReceiveAsync(wsClient2);
        OnDirectDeliveryData onDirectDeliveryData = AsPacketWrapper(receiveBuffer, result.Count).SystemPacketAsOnDirectDeliveryData();
        Assert.Equal(DirectDeliveryDataType.Whipher, onDirectDeliveryData.DataType);
        Assert.Equal(messageToSend, onDirectDeliveryData.Data);

        (receiveBuffer, result) = await ReceiveAsync(wsClient1);
        ResDirectDeliveryData resDirectDeliveryData = AsPacketWrapper(receiveBuffer, result.Count).SystemPacketAsResDirectDeliveryData();
        Assert.Equal(PacketErrorCodes.Success, resDirectDeliveryData.Result);

        await CloseAuthoredWebSocket(wsClient1);
        await CloseAuthoredWebSocket(wsClient2);    

    }
    [Fact]
    public async Task DirectDeliveryFailTest()
    {
        var wsClient1 = await CreateAuthoredWebSocket();

        var getUserInfoPacket = BuildReqUserInfoPacket();

        //Get Client1 User info
        await SendDataAsync(wsClient1, getUserInfoPacket);
        var (receiveBuffer, result) = await ReceiveAsync(wsClient1);
        ResUserInfo UserInfoClient1 = AsPacketWrapper(receiveBuffer, result.Count).SystemPacketAsResUserInfo();
        Guid player1Id = new();
        player1Id.FromGuidType(UserInfoClient1.PlayerId);
        Assert.NotEqual(Guid.Empty, player1Id);

        var player2Id = Guid.NewGuid();

        string messageToSend = "Hello Friend";
        var ReqDirectMessage = BuildReqDirectDeliveryData(player2Id, messageToSend, DirectDeliveryDataType.Whipher);
        await SendDataAsync(wsClient1, ReqDirectMessage);

        (receiveBuffer, result) = await ReceiveAsync(wsClient1);

        ResDirectDeliveryData resDirectDeliveryData = AsPacketWrapper(receiveBuffer, result.Count).SystemPacketAsResDirectDeliveryData();
        Assert.Equal(PacketErrorCodes.PlayerOffline, resDirectDeliveryData.Result);

        await CloseAuthoredWebSocket(wsClient1);

    }
}