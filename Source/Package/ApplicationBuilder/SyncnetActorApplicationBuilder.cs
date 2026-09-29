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

        // Player data extend feature enabled.
        if(_options.PlayerDataExtendDefinitionType is { } playerDataExtendDefinitionType &&
           _options.PlayerDataExtendType is { } playerDataExtendType &&
           _options.PlayerCustomBehaviorType is { } playerCustomBehaviorType)
        {
            Builder.Services.AddTransient(typeof(IPlayerDataExtendDefinition), playerDataExtendDefinitionType);
            Builder.Services.AddTransient(typeof(IPlayerCustomBehavior), playerCustomBehaviorType);
            Builder.Services.AddTransient(typeof(IPlayerDataExtend), playerDataExtendType);
        }
        else
        {
            throw new InvalidOperationException("Player extend data types are not configured. Please call UsePlayerExtendData<TExtendDefinitionType, TExtendDataStateType, TExtendDataBehaviorType>() to configure them.");
        }
        var webApp = Builder.Build();
        return new SyncnetActorApplication(webApp, _options);
    }
}
