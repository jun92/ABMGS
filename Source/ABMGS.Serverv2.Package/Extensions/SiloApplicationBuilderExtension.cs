using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Dashboard;
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
    private static void SyncnetPlatformSiloCommon(WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IPacketRouter, FlatBufferPacketRouter>();
        builder.Services.AddSingleton<IPacketContextFactory, PacketContextFactory>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();
        builder.Services.AddTransient<IPlayerModelRepository, RdbPlayerModelRepository>();
        builder.AddServiceDefaults();

        builder.UseOrleans(siloBuilder =>
        {
            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "SyncnetPlatformCluster";
                options.ServiceId = "SyncnetPlatformService";
            });
            siloBuilder.UseRedisClustering(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(
                    siloBuilder.Configuration.GetConnectionString("redis") ?? throw new InvalidOperationException());
                options.ConfigurationOptions.CertificateValidation += (sender, certificate, chain, sslPolicyErrors) => true;
            });
            siloBuilder.AddDashboard(options =>
            {
                options.CounterUpdateIntervalMs = 5000;
            });

        });
    }
    private static void SyncnetPlatformSiloDbContext(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<SyncnetDbContext>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("postgres"), optionBuilder =>
            {
                optionBuilder.MigrationsAssembly(typeof(SyncnetDbContext).Assembly.FullName);
            });
        });
    }
    public static void AddSyncnetPlatformSilo(this WebApplicationBuilder builder)
    {
        SyncnetPlatformSiloCommon(builder);
        SyncnetPlatformSiloDbContext(builder);
    }
    public static void AddSyncnetPlatformSilo(
        this WebApplicationBuilder builder,
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

    public void RegisterDbContext<DbContextType>(WebApplicationBuilder builder) where DbContextType : SyncnetDbContext
    {
        UseBuiltinDbContext = false;
        builder.Services.AddDbContextPool<DbContextType>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("postgres"), optionBuilder =>
            {
                optionBuilder.MigrationsAssembly(typeof(DbContextType).Assembly.FullName);
            });
        });
        builder.Services.AddScoped<SyncnetDbContext>(sp => sp.GetRequiredService<DbContextType>());
    }
}


