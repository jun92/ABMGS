using Google.FlatBuffers;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Tests;

public partial class ABMGS_TestMain : IAsyncLifetime
{
    [Fact]
    public async Task PlayerNameUpdateText()
    {
        var wsClient = await CreateAuthoredWebSocket();

        string RandomPlayerName = "Guest" + CreateRandomString(6);

        var (result, packetWrapper) = await SendAndReceive(wsClient, BuildUpdatePlayerNamePacket(RandomPlayerName));

        _output.WriteLine($"Count: {result.Count}");
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);

        Assert.Equal(SystemPacket.ResUpdatePlayerName, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResUpdatePlayerName().Result);

        (result, packetWrapper) = await SendAndReceive(wsClient, BuildReqUserInfoPacket());

        Assert.True(result.EndOfMessage);

        Assert.Equal(RandomPlayerName, packetWrapper.SystemPacketAsResUserInfo().Name);

        await CloseAuthoredWebSocket(wsClient);
    }

    [Fact]
    public async Task PlayerNameUpdateTwice_db_backward_compatibility_test()
    {
        var wsClient = await CreateAuthoredWebSocket("1234567");
        string RandomPlayerName = "Guest" + CreateRandomString(6);

        var (result, packetWrapper) = await SendAndReceive(wsClient, BuildUpdatePlayerNamePacket(RandomPlayerName));

        _output.WriteLine($"Count: {result.Count}");
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);

        Assert.Equal(SystemPacket.ResUpdatePlayerName, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResUpdatePlayerName().Result);

        (result, packetWrapper) = await SendAndReceive(wsClient, BuildReqUserInfoPacket());

        Assert.True(result.EndOfMessage);

        Assert.Equal(RandomPlayerName, packetWrapper.SystemPacketAsResUserInfo().Name);

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
        var (result, packetWrapper) = await ReceiveAsync(wsClient1);
        ResUserInfo UserInfoClient1 = packetWrapper.SystemPacketAsResUserInfo();
        Guid player1Id = new();
        player1Id.FromGuidType(UserInfoClient1.Id);
        Assert.NotEqual(Guid.Empty, player1Id);

        //Get Client2 User info
        await SendDataAsync(wsClient2, getUserInfoPacket);
        (result, packetWrapper) = await ReceiveAsync(wsClient2);
        ResUserInfo UserInfoClient2 = packetWrapper.SystemPacketAsResUserInfo();
        Guid player2Id = new();
        player2Id.FromGuidType(UserInfoClient2.Id);
        Assert.NotEqual(Guid.Empty, player2Id);

        // Two different accounts.
        Assert.NotEqual(player1Id, player2Id);


        string messageToSend = "Hello Friend";
        var ReqDirectMessage = BuildReqDirectDeliveryDataPacket(player2Id, messageToSend, DirectDeliveryDataType.Whipher);
        await SendDataAsync(wsClient1, ReqDirectMessage);

        (result, packetWrapper) = await ReceiveAsync(wsClient2);
        OnDirectDeliveryData onDirectDeliveryData = packetWrapper.SystemPacketAsOnDirectDeliveryData();
        Assert.Equal(DirectDeliveryDataType.Whipher, onDirectDeliveryData.DataType);
        Assert.Equal(messageToSend, onDirectDeliveryData.Data);

        (result, packetWrapper) = await ReceiveAsync(wsClient1);
        ResDirectDeliveryData resDirectDeliveryData = packetWrapper.SystemPacketAsResDirectDeliveryData();
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
        var (result, packetWrapper) = await ReceiveAsync(wsClient1);
        ResUserInfo UserInfoClient1 = packetWrapper.SystemPacketAsResUserInfo();
        Guid player1Id = new();
        player1Id.FromGuidType(UserInfoClient1.Id);
        Assert.NotEqual(Guid.Empty, player1Id);

        var player2Id = Guid.NewGuid();

        string messageToSend = "Hello Friend";
        var ReqDirectMessage = BuildReqDirectDeliveryDataPacket(player2Id, messageToSend, DirectDeliveryDataType.Whipher);
        await SendDataAsync(wsClient1, ReqDirectMessage);

        (result, packetWrapper) = await ReceiveAsync(wsClient1);

        ResDirectDeliveryData resDirectDeliveryData = packetWrapper.SystemPacketAsResDirectDeliveryData();
        Assert.Equal(PacketErrorCodes.PlayerOffline, resDirectDeliveryData.Result);

        await CloseAuthoredWebSocket(wsClient1);

    }
    [Fact]
    public async Task PlayerCustomDataUpdate()
    {
        var wsClient = await CreateAuthoredWebSocket();
        
        //Get current player custom data
        var reqUserInfo = BuildReqUserInfoPacket();
        await SendDataAsync(wsClient, reqUserInfo);
        var (result, packetWrapper) = await ReceiveAsync(wsClient);
        ResUserInfo UserInfoClient = packetWrapper.SystemPacketAsResUserInfo();
        
        
        var customData = PlayerCustomData.GetRootAsPlayerCustomData(new ByteBuffer(UserInfoClient.GetExtendDataArray()));
        Assert.Equal(1, customData.CustomLevel);
        Assert.Equal(33, customData.CustomExp);
        long prevCustomExp = customData.CustomExp;
        
        const string ActionType = "gainEXP";
        byte[] ActionParameters = BitConverter.GetBytes(100);
        var reqUserActionForUpdatePlayerCustomData = BuildReqUserActionForUpdatePlayerCustomData(ActionType, ActionParameters);
        
        await SendDataAsync(wsClient, reqUserActionForUpdatePlayerCustomData);
        
        (result, packetWrapper) = await ReceiveAsync(wsClient);
        ResUserActionForUpdatePlayerExtendData res = packetWrapper.SystemPacketAsResUserActionForUpdatePlayerExtendData();
        
        customData = PlayerCustomData.GetRootAsPlayerCustomData(new ByteBuffer(res.GetExtendDataArray()));
        
        Assert.Equal(prevCustomExp + 100, customData.CustomExp);

    }
}