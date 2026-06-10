
namespace SyncnetPlatform.Tests;

public partial class ABMGS_TestMain : IAsyncLifetime
{
    [Fact]
    public async Task HeathCheck()
    {
        var response = await _frontendHttpClient.GetAsync("/api/healthy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}