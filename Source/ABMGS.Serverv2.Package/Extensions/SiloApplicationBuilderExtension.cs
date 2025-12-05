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

namespace SyncnetPlatform.Extensions;

public static class SiloApplicationBuilderExtension
{
    private static void SyncnetPlatformSiloCommon(HostApplicationBuilder builder)
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
    }
    private static void SyncnetPlatformSiloDbContext(HostApplicationBuilder builder)
    {
        builder.Services.AddDbContextPool<SyncnetDbContext>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
            {
                optionBuilder.MigrationsAssembly(typeof(SyncnetDbContext).Assembly.FullName);
            });
        });
        //builder.Services.AddDbContextFactory<SyncnetDbContext>(opt =>
        //{
        //    opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
        //    {
        //        optionBuilder.MigrationsAssembly(typeof(SyncnetDbContext).Assembly.FullName);
        //    });
        //});
    }
    public static void AddSyncnetPlatformSilo(this HostApplicationBuilder builder)
    {
        SyncnetPlatformSiloCommon(builder);
        SyncnetPlatformSiloDbContext(builder);
    }
    public static void AddSyncnetPlatformSilo(
        this HostApplicationBuilder builder,
        Action<SyncnetSiloOptionsBuilder>? optionBuilder)
    {
        SyncnetPlatformSiloCommon(builder);

        SyncnetSiloOptionsBuilder options = new();
        options.UseBuiltinDbContext = true;
        if(optionBuilder != null)
        {
            optionBuilder(options);
        }

        if (options.UseBuiltinDbContext)
        {
            SyncnetPlatformSiloDbContext(builder);
        }
    }
}

public class SyncnetSiloOptionsBuilder
{
    public bool UseBuiltinDbContext { get; set; }

    public void RegisterDbContext<DbContextType>(HostApplicationBuilder builder) where DbContextType : SyncnetDbContext
    {
        UseBuiltinDbContext = false;
        builder.Services.AddDbContextPool<DbContextType>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
            {
                optionBuilder.MigrationsAssembly(typeof(DbContextType).Assembly.FullName);
            });
        });
        //builder.Services.AddDbContextFactory<DbContextType>(opt =>
        //{
        //    opt.UseNpgsql(builder.Configuration.GetConnectionString("npgsql"), optionBuilder =>
        //    {
        //        optionBuilder.MigrationsAssembly(typeof(DbContextType).Assembly.FullName);
        //    });
        //});
        builder.Services.AddScoped<SyncnetDbContext>(sp => sp.GetRequiredService<DbContextType>());
    }
}


