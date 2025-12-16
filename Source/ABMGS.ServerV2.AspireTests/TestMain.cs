using Google.FlatBuffers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using YamlDotNet.Core.Tokens;

namespace ABMGS.ServerV2.AspireTest;

public class TestMain
{
    private readonly ITestOutputHelper _output;
    private readonly Random _random = new Random();

    public TestMain(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public async Task DummyTest()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await builder.BuildAsync();
        await app.StartAsync();


        var httpClient = app.CreateHttpClient("orleans-frontend");
        //using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await app.ResourceNotifications.WaitForResourceHealthyAsync("orleans-frontend", CancellationToken.None);
        var response = await httpClient.GetAsync("/api/healthy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string randomString = new string(Enumerable.Repeat("0123456789", 6).Select(s => s[_random.Next(s.Length)]).ToArray());


        response = await httpClient.PostAsync($"/api/auth/token/guest/{randomString}", null);
        response.EnsureSuccessStatusCode();

        string token = await response.Content.ReadAsStringAsync();
       

        var wsUri = new UriBuilder(httpClient.BaseAddress!)
        {
            Scheme = httpClient.BaseAddress!.Scheme == "https" ? "wss" : "ws",
            Path = "/ws/gamesession"
        }.Uri;


        byte[] dataToSend = SyncnetPacketBuilder.Build<PingArgs>(new PingArgs(1));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.Ping, verifyPacket.SystemPacketType);

        var wsClient = new ClientWebSocket();
        await wsClient.ConnectAsync(wsUri, CancellationToken.None);

        wsClient.Options.SetRequestHeader("Authorization", $"Bearer {token}");

        Assert.Equal(WebSocketState.Open, wsClient.State);


        await wsClient.SendAsync(new ArraySegment<byte>(dataToSend), WebSocketMessageType.Binary, true, CancellationToken.None);

        //ArraySegment<byte> receiveBuffer = new ArraySegment<byte>();
        //WebSocketReceiveResult result = await wsClient.ReceiveAsync(receiveBuffer, CancellationToken.None);

        byte[] receiveBuffer = new byte[4096];
        WebSocketReceiveResult result = await wsClient.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);


        _output.WriteLine($"Count: {result.Count}");
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);

        PacketWrapper packetWrapper = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(receiveBuffer.Take(result.Count).ToArray()));

        Assert.Equal(SystemPacket.Pong, packetWrapper.SystemPacketType);
        Assert.Equal(2, packetWrapper.SystemPacketAsPong().Seq);

        await wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good Bye", CancellationToken.None);
    }
}
