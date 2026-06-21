using Microsoft.AspNetCore.Builder;
using SyncnetPlatform.ApplicationBuilder.Options;
using SyncnetPlatform.Extensions;

namespace SyncnetPlatform.ApplicationBuilder;

public class SyncnetActorApplication
{
    private readonly WebApplication _app;
    private readonly SyncnetBuilderOptions _options;
    public WebApplication WebApplication => _app;
    public SyncnetActorApplication(WebApplication app, SyncnetBuilderOptions options)
    {
        _app = app;
        _options = options;
    }


    public async Task RunAsync()
    {
        if(_options.AutoMigrateDatabase)
        {
            _app.SyncnetDbMigrate();
        }
        await _app.RunAsync();
    }
}
