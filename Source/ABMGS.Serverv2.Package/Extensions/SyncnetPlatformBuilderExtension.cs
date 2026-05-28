using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Orleans.Clustering.Redis;
using Orleans.Configuration;
using Orleans.Dashboard;
using Orleans.Hosting;
using Serilog;
using Serilog.Configuration;
using StackExchange.Redis;
using SyncnetPlatform.Authentication.GooglePlay;
using SyncnetPlatform.Authentication.SyncnetAuthProvider;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Extensions.Options;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Sessions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Repositories;
using System.Text;

namespace SyncnetPlatform.Extensions;

public static class SyncnetPlatformBuilderExtension
{
    // For Clients
    public static void AddSyncnetPlatformClient(
        this WebApplicationBuilder builder, 
        Action<SyncnetLoggerOption>? LoggerAction = null,
        Action<SyncnetTelemetryOption>? TelemetryAction = null)
    {
        AddSyncnetPlatformCommon(builder, LoggerAction, TelemetryAction);
       
        builder.Services.AddHttpClient();
        ConfigureAuthentication(builder);
        ConfigureOrleansAsClient(builder);
    }
    public static void AddSyncnetPlatformSilo(
        this WebApplicationBuilder builder, 
        Action<SyncnetLoggerOption>? LoggerAction = null,
        Action<SyncnetTelemetryOption>? TelemetryAction = null)
    {
        AddSyncnetPlatformCommon(builder, LoggerAction, TelemetryAction);
        ConfigureOrleansAsSilo(builder);
        SyncnetPlatformSiloDbContext(builder);
    }
    private static void AddSyncnetPlatformCommon(
        WebApplicationBuilder builder, 
        Action<SyncnetLoggerOption>? LoggerAction = null,
        Action<SyncnetTelemetryOption>? TelemetryAction = null)
    {
        if (LoggerAction != null) builder.Services.Configure(LoggerAction);
        if (TelemetryAction != null) builder.Services.Configure(TelemetryAction);

        builder.AddServiceDefaults();
        ConfigureGameServices(builder);
        ConfigureDatabase(builder);

        ConfigureLogger(builder);
        ConfigureTelemetry(builder);
    }

    private static void ConfigureGameServices(WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IGameSessionService, GameSessionService>();
        builder.Services.AddTransient<IPacketRouter, FlatBufferPacketRouter>();
        builder.Services.AddSingleton<IPacketContextFactory, PacketContextFactory>();
        builder.Services.AddTransient<ISystemPacketHandler, SystemPacketHandler>();
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<SyncnetDbContext>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("postgres"));

        });
        builder.Services.AddTransient<IPlayerModelRepository, RdbPlayerModelRepository>();
        builder.Services.AddTransient<IExternalIdentityRepository, RdbExternalIdentityRepository>();
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

    private static void ConfigureOrleansAsClient(WebApplicationBuilder builder)
    {
        builder.UseOrleansClient(configure =>
        {
            configure.Configure<ClusterOptions>(ClusterOptionsAction);
            configure.UseRedisClustering(options =>
            {
                options.ConfigurationOptions = GetRedisConfiguration(builder);
                options.ConfigurationOptions.CertificateValidation += VerifyRedisTls;
            });
            configure.AddDashboard();
        });
    }

    private static ConfigurationOptions GetRedisConfiguration(WebApplicationBuilder builder)
    {
        return ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("redis") 
            ?? throw new InvalidOperationException());
    }

    // 개발/테스트 환경에서 검증을 무조건 통과시키도록 true 반환
    private static bool VerifyRedisTls(
        object sender,
        System.Security.Cryptography.X509Certificates.X509Certificate? certificate,
        System.Security.Cryptography.X509Certificates.X509Chain? chain,
        System.Net.Security.SslPolicyErrors sslPolicyErrors) => true;
    private static void ConfigureOrleansAsSilo(WebApplicationBuilder builder)
    {
        builder.UseOrleans(siloBuilder =>
        {
            siloBuilder.Configure<ClusterOptions>(ClusterOptionsAction);
            siloBuilder.UseRedisClustering(options =>
            {
                options.ConfigurationOptions = GetRedisConfiguration(builder);
                options.ConfigurationOptions.CertificateValidation += VerifyRedisTls;

            });
            siloBuilder.AddDashboard(options =>
            {
                options.CounterUpdateIntervalMs = 5000;
            });
        });
    }
    private static void ClusterOptionsAction(ClusterOptions options)
    {
        options.ClusterId = "SyncnetPlatformCluster";
        options.ServiceId = "SyncnetPlatformService";
    }
    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IGooglePlayAuthenticationService, GooglePlayAuthenticationService>();
        builder.Services.AddTransient<ISyncnetAuthenticationService, PlayerAuthenticationService>();

        builder.Services.Configure<SyncnetAuthenticationOptions>(
            builder.Configuration.GetSection(nameof(SyncnetAuthenticationOptions))
        );
        builder.Services.Configure<GoogleAuthenticationConfiguration>(
            builder.Configuration.GetSection(nameof(GoogleAuthenticationConfiguration))
        );
        builder.Services.AddTransient<ISyncnetJwtAuthenticationService, SyncnetAuthenticationService>();

        // Guest Id cached as long as the backend is running.
        builder.Services.AddScoped<IGuestAuthenticationService, GuestAuthenticationService>();

        string IssuerSigningKey = builder.Configuration["SyncnetAuthenticationOptions:SecretKey"] ?? throw new InvalidOperationException("Secret key is no supplied");

        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options => {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["SyncnetAuthenticationOptions:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["SyncnetAuthenticationOptions:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(IssuerSigningKey))
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("GameSocketPolicy", policy => {
                policy.RequireAuthenticatedUser();
            });
        });

    }

    private static void ConfigureLogger(WebApplicationBuilder builder)
    {

        Serilog.Debugging.SelfLog.Enable(Console.Error);

        builder.Services.AddSerilog((services, loggerConfig) =>
        {
            SyncnetLoggerOption option = services.GetRequiredService<IOptions<SyncnetLoggerOption>>().Value;
            SyncnetTelemetryOption syncnetTelemetryOptions = services.GetRequiredService<IOptions<SyncnetTelemetryOption>>().Value;

            loggerConfig
                .MinimumLevel.Is(option.MinimumLevel)
                .MinimumLevel.Override("Microsoft", option.Override)
                .WriteTo.OpenTelemetry(option =>
                {
                    option.Endpoint = syncnetTelemetryOptions.Logging.Endpoint;
                    option.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                    option.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = builder.Environment.ApplicationName,
                    };

                }, ignoreEnvironment: true)
                .Enrich.FromLogContext();
            
            if(option.EnableConsole) loggerConfig.WriteTo.Console();
            if(option.IncludeThreadId) loggerConfig.Enrich.WithThreadId();

        });
    }
    private static void ConfigureTelemetry(WebApplicationBuilder builder)
    {
        SyncnetTelemetryOption syncnetTelemetryOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<SyncnetTelemetryOption>>().Value;
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("SyncnetPlatform");
                metrics.AddOtlpExporter(option =>
                {
                    option.Endpoint = new Uri(syncnetTelemetryOptions.Metric.Endpoint);
                    option.Protocol = syncnetTelemetryOptions.Metric.Protocol;
                });
            })
            .WithTracing(trace =>
            {
                trace
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("syncnet.traces")
                    .AddOtlpExporter(option =>
                    {
                        option.Endpoint = new Uri(syncnetTelemetryOptions.Trace.Endpoint);
                        option.Protocol = syncnetTelemetryOptions.Trace.Protocol;
                    });
            });
    }

    

    public static void UseFrontendSyncnetPlatform(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets();
        app.MapOrleansDashboard();
    }
}

