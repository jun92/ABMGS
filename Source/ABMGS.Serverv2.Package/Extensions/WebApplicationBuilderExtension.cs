using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        builder.Services.AddTransient<SystemPacketHandler>();
        builder.Services.AddTransient<FlatBufferPacketRouter>();
        builder.Services.AddTransient<ICustomPacketHandler, CustomPacketHandler>();

        // Orleans Configuration
        builder.UseOrleans(builder => {
            //builder.UseRedisClustering(options =>
            //{
            //    options.ConfigurationOptions = ConfigurationOptions.Parse(
            //        builder.Configuration.GetConnectionString("redis") ?? throw new InvalidOperationException());
            //});
            builder.UseLocalhostClustering();
        });
    }

    public static void AddCustomPacketHandler<CustomHandlerType>(this WebApplicationBuilder builder) where CustomHandlerType: ICustomPacketHandler
    {
       
    }
}
