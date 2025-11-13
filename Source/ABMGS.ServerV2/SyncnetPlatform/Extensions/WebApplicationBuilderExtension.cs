using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Sessions;
using ABMGS.ServerV2.SyncnetPlatform.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Network.Sessions;
using ABMGS.ServerV2.SyncnetPlatform.Network.Utils;
using System.Runtime.CompilerServices;

namespace ABMGS.ServerV2.SyncnetPlatform.Extensions;

public static class WebApplicationBuilderExtension
{
    public static void UseSyncnetPlatform(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IGameSessionService, GameSessionService>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();
        builder.Services.AddTransient<FlatBufferPacketRouter>();
    }

    public static void AddCustomPacketHandler<CustomHandlerType>(this WebApplicationBuilder builder) where CustomHandlerType: ICustomPacketHandler
    {
        builder.Services.AddTransient(typeof(CustomHandlerType));
    }
}
