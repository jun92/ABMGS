using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;

namespace ABMGS.ServerV2.AspireTest;

public class AspireAppFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;

    public ResourceNotificationService ResourceNotificationService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        App = await builder.BuildAsync();

        ResourceNotificationService = App.Services.GetRequiredService<ResourceNotificationService>();

        await App.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }

    public async Task<HttpClient> CreateHttpClientToFrontEnd(string frontendName)
    {
        var httpClient = App.CreateHttpClient(frontendName);
        await App.ResourceNotifications.WaitForResourceHealthyAsync(frontendName);
        return httpClient;
    }
}
