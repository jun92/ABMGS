using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using StackExchange.Redis;
using SyncnetPlatform.Actors;
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
        builder.Services.AddSingleton<SendQueueService>();
        builder.Services.AddTransient<ICustomPacketHandler, CustomPacketHandler>();
        builder.UseOrleansClient(configure =>
        {
            configure.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "SyncnetPlatformCluster";
                options.ServiceId = "SyncnetPlatformService";
            });
            configure.UseRedisClustering(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(
                    builder.Configuration.GetConnectionString("redis") ?? throw new InvalidOperationException());
            });
        });
        
    }

    public static void AddCustomPacketHandler<CustomHandlerType>(this WebApplicationBuilder builder) where CustomHandlerType: ICustomPacketHandler
    {
       
    }
}

public static class HostApplicationBuilderExtension
{
    public static void UseSyncnetPlatform(this HostApplicationBuilder appBuilder)
    {
        appBuilder.UseOrleans(builder =>
        {
            builder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "SyncnetPlatformCluster";
                options.ServiceId = "SyncnetPlatformService";
            });
            builder.UseRedisClustering(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(
                    appBuilder.Configuration.GetConnectionString("redis") ?? throw new InvalidOperationException());
            });
        });
    }
}
