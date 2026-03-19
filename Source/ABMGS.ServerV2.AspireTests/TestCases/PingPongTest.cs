using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace ABMGS.ServerV2.AspireTest;

public partial class ABMGS_TestMain : IAsyncLifetime
{
    [Fact]
    public async Task PingPongTestWithGuestAuth()
    {
        var wsClient = await CreateAuthoredWebSocket();

        var dataToSend = BuildPingPacket(1);
        await SendDataAsync(wsClient, dataToSend);

        var (receiveBuffer, result) = await ReceiveAsync(wsClient);

        _output.WriteLine($"Count: {result.Count}");
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.Pong, AsPacketWrapper(receiveBuffer, result.Count).SystemPacketType);
        Pong pong = AsPacketWrapper(receiveBuffer,result.Count).SystemPacketAsPong();
        
        Assert.Equal(2, pong.Seq);

        await CloseAuthoredWebSocket(wsClient);
    }
}