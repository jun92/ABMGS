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

        var (result, packetWrapper) = await SendAndReceive(wsClient, BuildPingPacket(1));

        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);
        Assert.Equal(SystemPacket.Pong, packetWrapper.SystemPacketType);

        Pong pong = packetWrapper.SystemPacketAsPong();
        
        Assert.Equal(2, pong.Seq);

        await CloseAuthoredWebSocket(wsClient);
    }
}