using Microsoft.AspNetCore.Builder;
using SyncnetPlatform.ApplicationBuilder.Options;
using SyncnetPlatform.Extensions;

namespace SyncnetPlatform.ApplicationBuilder;

public class SyncnetFrontApplication
{
    private readonly WebApplication _app;
    private readonly SyncnetBuilderOptions _options;
    public WebApplication WebApplication => _app;
    public SyncnetFrontApplication(WebApplication app, SyncnetBuilderOptions options)
    {
        _app = app;
        _options = options;
    }
    public async Task RunAsync()
    {
        // Front에 필요한 기본 파이프라인 구성 자동화
        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFrontendSyncnetPlatform();

        _app.MapControllers();
        await _app.RunAsync();
    }
}