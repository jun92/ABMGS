using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using StackExchange.Redis;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Sessions;
using SyncnetPlatform.Network.Utils;
using System.Threading.Tasks;

namespace SyncnetPlatform.Extensions;

public static class WebApplicationBuilderExtension
{
    // For Clients
    public static void UseSyncnetPlatform(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IGameSessionService, GameSessionService>();
        builder.Services.AddTransient<IPacketRouter, FlatBufferPacketRouter>();
        builder.Services.AddSingleton<IPacketContextFactory, PacketContextFactory>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();

        builder.Services.AddDbContextPool<SyncnetDbContext>(opt => {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
            {
           //     optionBuilder.MigrationsAssembly("ABMGS.ServerV2.Migrations");
            });
        });

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

        // Database migraion.
        //WebApplication app = builder.Build();
        //using IServiceScope scope = app.Services.CreateScope();
        //SyncnetDbContext context = scope.ServiceProvider.GetRequiredService<SyncnetDbContext>();
        //context.Database.Migrate();
        
    }
}
// For Silos
public static class HostApplicationBuilderExtension
{
    public static void UseSyncnetPlatform(this HostApplicationBuilder builder)
    {
        builder.Services.AddTransient<IPacketRouter, FlatBufferPacketRouter>();
        builder.Services.AddSingleton<IPacketContextFactory, PacketContextFactory>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();
        builder.Services.AddDbContextPool<SyncnetDbContext>(opt => {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
            {
             //   optionBuilder.MigrationsAssembly("ABMGS.ServerV2.Migrations");
            });
        });

        builder.UseOrleans(builder =>
        {
            builder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "SyncnetPlatformCluster";
                options.ServiceId = "SyncnetPlatformService";
            });
            builder.UseRedisClustering(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(
                    builder.Configuration.GetConnectionString("redis") ?? throw new InvalidOperationException());
            });
        });

        // Database migraion.
        IHost app = builder.Build();
        using IServiceScope scope = app.Services.CreateScope();
        SyncnetDbContext context = scope.ServiceProvider.GetRequiredService<SyncnetDbContext>();
        context.Database.MigrateAsync().GetAwaiter().GetResult();
    }
}
