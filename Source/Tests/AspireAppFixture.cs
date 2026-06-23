using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Threading.Tasks;

namespace SyncnetPlatform.Tests;
public class AspireAppFixture : IAsyncLifetime
{
    public DistributedApplication? App { get; private set; }
    public ResourceNotificationService? ResourceNotificationService { get; private set; }

    private string? _remoteEndpoint;

    public async Task InitializeAsync()
    {
        _remoteEndpoint = Environment.GetEnvironmentVariable("TEST_REMOTE_ENDPOINT");

        if (!string.IsNullOrWhiteSpace(_remoteEndpoint))
        {
            // Skip starting local Aspire app, we are targeting a remote endpoint.
            return;
        }

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        App = await builder.BuildAsync();

        ResourceNotificationService = App.Services.GetRequiredService<ResourceNotificationService>();

        await App.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (App != null)
        {
            await App.DisposeAsync();
        }
    }

    public async Task<HttpClient> CreateHttpClientToFrontEnd(string frontendName)
    {
        if (!string.IsNullOrWhiteSpace(_remoteEndpoint))
        {
            return new HttpClient{ BaseAddress = new Uri(_remoteEndpoint.TrimEnd('/') + "/")};
        }

        if (App == null)
        {
            throw new InvalidOperationException("Aspire application has not been initialized.");
        }

        var localHttpClient = App.CreateHttpClient(frontendName, "http");
        await App.ResourceNotifications.WaitForResourceHealthyAsync(frontendName);
        return localHttpClient;
    }

}