using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using SyncnetPlatform.Network.Utils;

namespace ABMGS.ServerV2.AspireTest;

public class TestMain
{
    [Fact]
    public async Task DummyTest()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();

        builder.Services.ConfigureHttpClientDefaults(clientBuilder => {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await builder.BuildAsync();
        await app.StartAsync();


        var httpClient = app.CreateHttpClient("orleans-frontend");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await app.ResourceNotifications.WaitForResourceHealthyAsync("orleans-frontend", cts.Token);
        var response = await httpClient.GetAsync("/alive");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var wsUri = new UriBuilder(httpClient.BaseAddress!)
        {
            Scheme = httpClient.BaseAddress!.Scheme == "https" ? "wss" : "ws",
            Path = "/gamesession"
        }.Uri;


        var wsClient = new ClientWebSocket();
        await wsClient.ConnectAsync(wsUri, CancellationToken.None);

        Assert.Equal(WebSocketState.Open, wsClient.State);

        await wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good Bye", cancellationToken: CancellationToken.None);




    }
}
