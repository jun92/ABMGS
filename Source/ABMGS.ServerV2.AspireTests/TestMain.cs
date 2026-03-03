using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Google.FlatBuffers;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;
using YamlDotNet.Core.Tokens;

namespace ABMGS.ServerV2.AspireTest;

[Collection("AspireCollection")]
public class ABMGS_TestMain : IAsyncLifetime
{
    private readonly AspireAppFixture _appFixture;
    private readonly ITestOutputHelper _output;
    private readonly Random _random = new Random();
    private HttpClient _frontendHttpClient;

    public ABMGS_TestMain(AspireAppFixture fixture, ITestOutputHelper output)
    {
        _appFixture = fixture;
        _output = output;
    }
    public async Task InitializeAsync()
    {
        _frontendHttpClient = await _appFixture.CreateHttpClientToFrontEnd("orleans-frontend");
    }
    public Task DisposeAsync()
    {
        _frontendHttpClient?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HeathCheck()
    {
        var response = await _frontendHttpClient.GetAsync("/api/healthy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PingPongTestWithGuestAuth()
    {
        var wsUri = new UriBuilder(_frontendHttpClient.BaseAddress!)
        {
            Scheme = _frontendHttpClient.BaseAddress!.Scheme == "https" ? "wss" : "ws",
            Path = "/ws/gamesession"
        }.Uri;

        var dataToSend = BuildPingPacket(1);
        var token = await GetGuestAuthToken();

        var wsClient = await OpenAuthoredWebSocket(wsUri, token);

        await wsClient.SendAsync(new ArraySegment<byte>(dataToSend), WebSocketMessageType.Binary, true, CancellationToken.None);
     
        byte[] receiveBuffer = new byte[4096];
        WebSocketReceiveResult result = await wsClient.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

        _output.WriteLine($"Count: {result.Count}");
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);

        PacketWrapper packetWrapper = PacketWrapper.GetRootAsPacketWrapper(
            new ByteBuffer(receiveBuffer.Take(result.Count).ToArray()));

        Assert.Equal(SystemPacket.Pong, packetWrapper.SystemPacketType);
        Assert.Equal(2, packetWrapper.SystemPacketAsPong().Seq);

        await CloseAuthoredWebSocket(wsClient);
    }

    //[Fact]
    //public async Task CreateNewPlayerTest()
    //{
    //    var wsUri = new UriBuilder(_frontendHttpClient.BaseAddress!)
    //    {
    //        Scheme = _frontendHttpClient.BaseAddress!.Scheme == "https" ? "wss" : "ws",
    //        Path = "/ws/gamesession"
    //    }.Uri;

    //    var token = await GetGuestAuthToken();

    //    var wsClient = await OpenAuthoredWebSocket(wsUri, token);
    //    var dataToSend = BuildCreateNewPlayerPacket();
    //    await wsClient.SendAsync(new ArraySegment<byte>(dataToSend), WebSocketMessageType.Binary, true, CancellationToken.None);

    //    byte[] receiveBuffer = new byte[4096];
    //    WebSocketReceiveResult result = await wsClient.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

    //    Assert.True(result.EndOfMessage);
    //    Assert.NotEqual(0, result.Count);

    //    PacketWrapper packetWrapper = PacketWrapper.GetRootAsPacketWrapper(
    //        new ByteBuffer(receiveBuffer.Take(result.Count).ToArray()));

    //    Assert.Equal(SystemPacket.ResCreateNewUser, packetWrapper.SystemPacketType);

    //    await CloseAuthoredWebSocket(wsClient);
    //}

    protected async Task<ClientWebSocket> OpenAuthoredWebSocket(Uri wsUri, string token)
    {
        var wsClient = new ClientWebSocket();
        wsClient.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        await wsClient.ConnectAsync(wsUri, CancellationToken.None);
        Assert.Equal(WebSocketState.Open, wsClient.State);
        return wsClient;
    }
    protected async Task CloseAuthoredWebSocket(ClientWebSocket socket)
    {
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good Bye", CancellationToken.None);

    }

    protected async Task<string> GetGuestAuthToken()
    {
        string guestId = CreateRandomString(6);
        var response = await _frontendHttpClient.PostAsync($"/api/auth/token/guest/{guestId}", null);
        response.EnsureSuccessStatusCode();
        string token = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<string>(token) ?? throw new InvalidOperationException("Received null or invalid token from authentication service.");

        //return token.Replace("\"", "");
    }
    protected string CreateRandomString(int length)
    {
        return new string(
            Enumerable
                .Repeat("0123456789", length)
                .Select(s => s[_random.Next(s.Length)])
                .ToArray());
    }
    protected byte[] BuildPingPacket(int seq = 1)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<PingArgs>(new PingArgs(seq));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.Ping, verifyPacket.SystemPacketType);
        return dataToSend;
    }
    protected byte[] BuildCreateNewPlayerPacket()
    {
        string RandomTestUserName = "RandomTestUserName" + CreateRandomString(8);
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqCreateNewUserArgs>(new ReqCreateNewUserArgs(RandomTestUserName));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqCreateNewUser, verifyPacket.SystemPacketType);
        return dataToSend;

    }


}


