using Microsoft.Extensions.DependencyInjection;
using SyncnetPlatform.Actors;
using SyncnetPlatform.ApplicationBuilder.Options;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Extensions;
using System.Reflection;

namespace SyncnetPlatform.ApplicationBuilder;

public class SyncnetActorApplicationBuilder : SyncnetBaseApplicationBuilder<SyncnetActorApplicationBuilder, SyncnetActorApplication>
{
    public readonly SyncnetBuilderOptions _options = new();
    public SyncnetActorApplicationBuilder(string[] args) : base(args)
    {
    }

    public SyncnetActorApplicationBuilder ConfigureActor(Action<SyncnetBuilderOptions> opt)
    {
        opt(_options);
        return this;
    }

    public override SyncnetActorApplication Build()
    {
        Builder.AddSyncnetPlatformSilo(_options.LoggerConfigure, _options.TelemetryConfigure);
        if(_options.PlayerDataExtendCreateType is Type PlayerDataExtendType )
            Builder.Services.AddTransient(typeof(IPlayerDataExtendCreater), PlayerDataExtendType);
        if (_options.PlayerCustomBehaviorType is Type PlayerCustomBehaviorType)
            Builder.Services.AddTransient(typeof(IPlayerCustomBehavior), PlayerCustomBehaviorType);

        var webApp = Builder.Build();
        return new SyncnetActorApplication(webApp, _options);
    }
    
    protected string? GetAssemblyNameForEfCoreMigration()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        return entryAssembly.GetName().Name;
    }
}
