using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using StackExchange.Redis;
using SyncnetPlatform.Authentication.GooglePlay;
using SyncnetPlatform.Authentication.SyncnetAuthProvider;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Sessions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Repositories;
using System.Threading.Tasks;

namespace SyncnetPlatform.Extensions;

public static class FrontendApplicationBuilderExtension
{
    // For Clients
    public static void AddSyncnetPlatformFrontend(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IGameSessionService, GameSessionService>();
        builder.Services.AddTransient<IPacketRouter, FlatBufferPacketRouter>();
        builder.Services.AddSingleton<IPacketContextFactory, PacketContextFactory>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();

        builder.Services.AddDbContextPool<SyncnetDbContext>(opt => {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("SyncnetPlatform"));
        });
        builder.Services.AddTransient<IPlayerModelRepository, RdbPlayerModelRepository>();
        builder.Services.AddTransient<IGooglePlayAuthenticationService, GooglePlayAuthenticationService>();
        builder.Services.AddTransient<ISyncnetAuthenticationService, PlayerAuthenticationService>();
        builder.Services.AddHttpClient();
        var aaa = builder.Configuration.GetSection(nameof(SyncnetAuthenticationOptions));

        builder.Services.Configure<SyncnetAuthenticationOptions>(
            builder.Configuration.GetSection(nameof(SyncnetAuthenticationOptions))
        );
        builder.Services.AddTransient<ISyncnetJwtAuthenticationService, SyncnetAuthenticationService>();

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
}

