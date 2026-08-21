using SyncnetPlatform.Actors;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Extensions.Options;
using System;

namespace SyncnetPlatform.ApplicationBuilder.Options;

public class SyncnetBuilderOptions
{
    public Action<SyncnetTelemetryOption>? TelemetryConfigure { get; set; } = null;
    public Action<SyncnetLoggerOption>? LoggerConfigure { get; set; } = null;
    public bool AutoMigrateDatabase { get; set; } = false;

    public Type? PlayerDataExtendCreateType { get; private set; } = null;
    public Type? PlayerCustomBehaviorType { get; private set; } = null;
    public void UsePlayerDataExtendCreator<T>() where T: class, IPlayerDataExtendCreater
    {
        PlayerDataExtendCreateType = typeof(T);
    }
    public void UsePlayerCustomBehavior<T>() where T: class, IPlayerCustomBehavior
    {
        PlayerCustomBehaviorType = typeof(T);
    }
}
