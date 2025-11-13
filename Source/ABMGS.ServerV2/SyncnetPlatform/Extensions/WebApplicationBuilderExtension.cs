using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Sessions;
using SyncnetPlatform.Network.Utils;

namespace SyncnetPlatform.Extensions;

public static class WebApplicationBuilderExtension
{
    public static void UseSyncnetPlatform(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IGameSessionService, GameSessionService>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();
        builder.Services.AddTransient<FlatBufferPacketRouter>();
        builder.Services.AddTransient<ICustomPacketHandler, CustomPacketHandler>();
    }

    public static void AddCustomPacketHandler<CustomHandlerType>(this WebApplicationBuilder builder) where CustomHandlerType: ICustomPacketHandler
    {
       // builder.Services.AddTransient(typeof(CustomHandlerType));
    }
}
