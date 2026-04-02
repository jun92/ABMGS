using Google.FlatBuffers;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ABMGS.ServerV2.AspireTest;

public partial class ABMGS_TestMain : IAsyncLifetime
{

    protected async Task<(WebSocketReceiveResult, PacketWrapper)> SendAndReceive(ClientWebSocket client, byte[] packet)
    {
        await SendDataAsync(client, packet);
        return await ReceiveAsync(client);
    }
    protected async Task<(WebSocketReceiveResult, PacketWrapper)> ReceiveAsync(ClientWebSocket client)
    {
        byte[] receiveBuffer = new byte[4096];
        WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), defaultTimeoutToken.Token);
        Assert.False(defaultTimeoutToken.IsCancellationRequested);
        Assert.True(result.EndOfMessage);
        Assert.NotEqual(0, result.Count);
        PacketWrapper packetWrapper = AsPacketWrapper(receiveBuffer, result.Count);
        return (result, packetWrapper);
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
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good Bye", CancellationToken.None);
    }

    protected async Task<ClientWebSocket> CreateAuthoredWebSocket()
    {
        var wsUri = new UriBuilder(_frontendHttpClient.BaseAddress!)
        {
            Scheme = _frontendHttpClient.BaseAddress!.Scheme == "https" ? "wss" : "ws",
            Path = "/ws/gamesession"
        }.Uri;
        var token = await GetGuestAuthToken();
        return await OpenAuthoredWebSocket(wsUri, token);
    }

    protected async Task<string> GetGuestAuthToken()
    {
        string guestId = CreateRandomString(6);
        var response = await _frontendHttpClient.PostAsync($"/api/auth/token/guest/{guestId}", null);
        response.EnsureSuccessStatusCode();
        string token = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<string>(token) ?? throw new InvalidOperationException("Received null or invalid token from authentication service.");
    }
    protected string CreateRandomString(int length)
    {
        return new string(
            Enumerable
                .Repeat("0123456789", length)
                .Select(s => s[_random.Next(s.Length)])
                .ToArray());
    }


    protected async Task SendDataAsync(ClientWebSocket client, byte[] dataToSend)
    {
        await client.SendAsync(new ArraySegment<byte>(dataToSend), WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    protected PacketWrapper AsPacketWrapper(byte[] receiveBuffer, int count)
    {
        return PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(receiveBuffer.Take(count).ToArray()));
    }
}
