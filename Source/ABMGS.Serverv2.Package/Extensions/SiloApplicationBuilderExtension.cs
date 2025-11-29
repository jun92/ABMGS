using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using StackExchange.Redis;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Extensions;


public static class SiloApplicationBuilderExtension
{
    public static void AddSyncnetPlatformSilo(
        this HostApplicationBuilder builder,
        Action<SyncnetSiloOptionsBuilder> optionBuilder)
    {
        builder.Services.AddTransient<IPacketRouter, FlatBufferPacketRouter>();
        builder.Services.AddSingleton<IPacketContextFactory, PacketContextFactory>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();
        builder.Services.AddTransient<IPlayerModelRepositoy, rdbPlayerModelRepository>();

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
        SyncnetSiloOptionsBuilder options = new();
        options.UseBuiltinDbContext = true;

        optionBuilder(options);

        if (options.UseBuiltinDbContext)
        {
            builder.Services.AddDbContextPool<SyncnetDbContext>(opt =>
            {
                opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
                {
                    optionBuilder.MigrationsAssembly(typeof(SyncnetDbContext).Assembly.FullName);
                });
            });
            using (var scope = builder.Services.BuildServiceProvider().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SyncnetDbContext>();
                db.Database.Migrate();
            }
        }
    }
}

public class SyncnetSiloOptionsBuilder
{
    public bool UseBuiltinDbContext { get; set; }

    public void RegisterDbContext<DbContextType>(HostApplicationBuilder builder) where DbContextType : DbContext
    {
        builder.Services.AddDbContextPool<DbContextType>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
            {
                optionBuilder.MigrationsAssembly(typeof(DbContextType).Assembly.FullName);
            });
        });
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DbContextType>();
            db.Database.Migrate();
        }
    }
}


