using Microsoft.Extensions.DependencyInjection;
using SyncnetPlatform.Actors;
using SyncnetPlatform.ApplicationBuilder.Options;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Extensions;
using System;
using System.Reflection;

namespace SyncnetPlatform.ApplicationBuilder;

public class SyncnetActorApplicationBuilder : SyncnetBaseApplicationBuilder<SyncnetActorApplicationBuilder, SyncnetActorApplication>
{
    private readonly SyncnetBuilderOptions _options = new();
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
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null || entryAssembly.GetName().Name == "ef")
        {
            entryAssembly = Assembly.GetCallingAssembly();
        }
        Builder.AddSyncnetPlatformSilo(_options.LoggerConfigure, _options.TelemetryConfigure, entryAssembly.GetName().Name);

        if(_options.PlayerDataExtendCreateType is { } playerDataExtendType )
            Builder.Services.AddTransient(typeof(IPlayerDataExtendCreater), playerDataExtendType);
        if (_options.PlayerCustomBehaviorType is { } playerCustomBehaviorType)
            Builder.Services.AddTransient(typeof(IPlayerCustomBehavior), playerCustomBehaviorType);

        var webApp = Builder.Build();
        return new SyncnetActorApplication(webApp, _options);
    }
}
