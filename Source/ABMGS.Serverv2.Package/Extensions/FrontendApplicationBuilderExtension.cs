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

public static partial class FrontendApplicationBuilderExtension
{
    // For Clients
    public static void AddSyncnetPlatformFrontend(
        this WebApplicationBuilder builder, 
        Action<SyncnetLoggerOption>? LoggerAction = null,
        Action<SyncnetTelemetryOption>? TelemetryAction = null)
    {
        ConfigureGameServices(builder);
        ConfigureDatabase(builder);
       
        builder.Services.AddHttpClient();
        builder.AddServiceDefaults();

        ConfigureAuthentication(builder);
        ConfigureOrleans(builder);
        
        
        ConfigureLogger(builder, LoggerAction);
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

    private static void ConfigureOrleans(WebApplicationBuilder builder)
    {
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
                options.ConfigurationOptions.CertificateValidation += (sender, certificate, chain, sslPolicyErrors) => true;
            });
            configure.AddDashboard();
        });
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

    private static void ConfigureLogger(WebApplicationBuilder builder, Action<SyncnetLoggerOption>? LoggerAction = null)
    {
        if (LoggerAction != null)
        {
            builder.Services.Configure(LoggerAction);
        }

        builder.Host.UseSerilog((context, services, LoggerConfiguration) => 
        { 
            SyncnetLoggerOption option = services.GetRequiredService<IOptions<SyncnetLoggerOption>>().Value;
            LoggerConfiguration
                .MinimumLevel.Is(option.MinimumLevel)
                .MinimumLevel.Override("Microsoft", option.Override)
                .Enrich.FromLogContext();
            if(option.EnableConsole) LoggerConfiguration.WriteTo.Console();
            if(option.IncludeThreadId) LoggerConfiguration.Enrich.WithThreadId();

        });
    }
    private static void ConfigureTelemetry(WebApplicationBuilder builder, Action<SyncnetTelemetryOption>? telemetryAction)
    {
        if (telemetryAction != null)
        {
            builder.Services.Configure(telemetryAction);
            SyncnetTelemetryOption syncnetTelemetryOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<SyncnetTelemetryOption>>().Value;
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;

                logging.AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri(syncnetTelemetryOptions.Logging.Endpoint);
                    opt.Protocol = syncnetTelemetryOptions.Logging.Protocol;
                });
            });
            builder.Services.AddOpenTelemetry()
                .WithLogging(logging =>
                {
                    logging.AddOtlpExporter();

                })
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
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
                    trace.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
                        .AddSource("syncnet.traces")
                        .AddOtlpExporter(option =>
                        {
                            option.Endpoint = new Uri(syncnetTelemetryOptions.Trace.Endpoint);
                            option.Protocol = syncnetTelemetryOptions.Trace.Protocol;
                        });
                });
        }
    }

    public static void UseFrontendSyncnetPlatform(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets();
        app.MapOrleansDashboard();
    }
}

