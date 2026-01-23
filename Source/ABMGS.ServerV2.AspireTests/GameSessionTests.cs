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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using YamlDotNet.Core.Tokens;

namespace ABMGS.ServerV2.AspireTest;

[Collection("AspireCollection")]
public class GameSessionTests : IAsyncLifetime
{
    private readonly AspireAppFixture _appFixture;
    private readonly ITestOutputHelper _output;
    private readonly Random _random = new Random();
    private HttpClient _frontendHttpClient = null!;

    private const string FrontendResourceName = "orleans-frontend";
    private const string HealthyPath = "/api/healthy";
    private const string GameSessionPath = "/ws/gamesession";
    private const string GuestAuthPath = "/api/auth/token/guest/";

    public GameSessionTests(AspireAppFixture fixture, ITestOutputHelper output)
    {
        _appFixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _frontendHttpClient = await _appFixture.CreateHttpClientToFrontEnd(FrontendResourceName);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HeathCheck()
    {
        var response = await _frontendHttpClient.GetAsync(HealthyPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PingPongTestWithGuestAuth()
    {
        var wsUri = new UriBuilder(_frontendHttpClient.BaseAddress!)
        {
            Scheme = _frontendHttpClient.BaseAddress!.Scheme == "https" ? "wss" : "ws",
            Path = GameSessionPath
        }.Uri;

        var dataToSend = BuildPingPacket(1);
        var token = await GetGuestAuthToken();

        using var wsClient = await OpenAuthoredWebSocket(wsUri, token);

        await wsClient.SendAsync(new ArraySegment<byte>(dataToSend), WebSocketMessageType.Binary, true, CancellationToken.None);
     
        byte[] receiveBuffer = new byte[4096];
        WebSocketReceiveResult result = await wsClient.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

        _output.WriteLine($"Count: {result.Count}");
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);

        byte[] actualData = new byte[result.Count];
        Array.Copy(receiveBuffer, actualData, result.Count);
        PacketWrapper packetWrapper = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(actualData));

        Assert.Equal(SystemPacket.Pong, packetWrapper.SystemPacketType);
        Assert.Equal(2, packetWrapper.SystemPacketAsPong().Seq);

        await CloseAuthoredWebSocket(wsClient);
    }

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
        if (socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good Bye", CancellationToken.None);
        }
    }

    protected async Task<string> GetGuestAuthToken()
    {
        string guestId = CreateRandomString(6);
        // Using Path.Combine or string interpolation carefully
        var response = await _frontendHttpClient.PostAsync($"{GuestAuthPath}{guestId}", null);
        response.EnsureSuccessStatusCode();
        string token = await response.Content.ReadAsStringAsync();
        // The API likely returns "token" (quoted string).
        return token.Trim('"');
    }

    protected string CreateRandomString(int length)
    {
        const string chars = "0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    protected byte[] BuildPingPacket(int seq = 1)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<PingArgs>(new PingArgs(seq));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.Ping, verifyPacket.SystemPacketType);
        return dataToSend;
    }
}
