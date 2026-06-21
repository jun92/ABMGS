using Microsoft.Extensions.DependencyInjection;
using SyncnetPlatform.ApplicationBuilder.Options;
using SyncnetPlatform.Extensions;

namespace SyncnetPlatform.ApplicationBuilder;

public class SyncnetFrontApplicationBuilder : SyncnetBaseApplicationBuilder<SyncnetFrontApplicationBuilder, SyncnetFrontApplication>
{
    public readonly SyncnetBuilderOptions _options = new();
    public SyncnetFrontApplicationBuilder(string[] args) : base(args)
    {

    }

    public SyncnetFrontApplicationBuilder ConfigureFront(Action<SyncnetBuilderOptions> opt)
    {
        opt(_options);
        return this;
    }


    public override SyncnetFrontApplication Build()
    {
        Builder.Services.AddControllers();
        Builder.AddSyncnetPlatformClient(_options.LoggerConfigure, _options.TelemetryConfigure);
        var webApp = Builder.Build();
        return new SyncnetFrontApplication(webApp, _options);
        
    }
}
