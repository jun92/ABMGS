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

    // For creation of player extend data.
    public Type? PlayerDataExtendDefinitionType { get; private set; } = null;
    
    // For player custom action handler
    public Type? PlayerCustomBehaviorType { get; private set; } = null;
    public Type? PlayerDataExtendType { get; private set; } = null;

    public void UsePlayerDataExtend<TExtendDefinitionType, TExtendDataStateType, TExtendDataBehaviorType>() 
        where TExtendDefinitionType : class, IPlayerDataExtendDefinition
        where TExtendDataStateType : class, IPlayerExtendData
        where TExtendDataBehaviorType : class, IPlayerCustomBehavior
    {
        PlayerDataExtendDefinitionType = typeof(TExtendDefinitionType);
        PlayerDataExtendType = typeof(TExtendDataBehaviorType);
        PlayerCustomBehaviorType = typeof(TExtendDataStateType);
    }
    
    // play room's custom state
    public Type? PlayRoomCustomStateType { get; private set; } = null;
    public Type? PlayRoomCustomEventHandlerType { get; private set; } = null;
    public void UsePlayRoom<TPlayRoomCustomStateType, TPlayRoomCustomEventHandlerType>() 
        where TPlayRoomCustomStateType : class, IPlayRoomCustomState
        where TPlayRoomCustomEventHandlerType : class, IPlayRoomCustomEventHandler
    {
        PlayRoomCustomStateType = typeof(TPlayRoomCustomStateType);
        PlayRoomCustomEventHandlerType = typeof(TPlayRoomCustomEventHandlerType);
    }

}
