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
using SyncnetPlatform.Network.Sessions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Repositories;
using SyncnetPlatform.Utils;
using SyncnetPlatform.Utils.Telemetry;
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

        ConfigureLogger(builder, LoggerAction, TelemetryAction);
        ConfigureTelemetry(builder, TelemetryAction);
    }

    private static void ConfigureGameServices(WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IGameSessionService, GameSessionService>();
        builder.Services.AddTransient<IPacketRouter, FlatBufferPacketRouter>();
        builder.Services.AddSingleton<SyncnetMetricsService>();
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
            configure.AddIncomingGrainCallFilter<SyncnetIncomingGrainCallFilter>();
            configure.AddOutgoingGrainCallFilter<SyncnetOutgoingGrainCallFilter>();
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
        System.Net.Security.SslPolicyErrors sslPolicyErrors)

    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return env == "Development";
    }
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
            siloBuilder.AddIncomingGrainCallFilter<SyncnetIncomingGrainCallFilter>();
            siloBuilder.AddOutgoingGrainCallFilter<SyncnetOutgoingGrainCallFilter>();
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

    private static void ConfigureLogger(
        WebApplicationBuilder builder, 
        Action<SyncnetLoggerOption>? LoggerAction = null,
        Action<SyncnetTelemetryOption>? TelemetryAction = null)
    {
        Serilog.Debugging.SelfLog.Enable(Console.Error);

        builder.Services.AddSerilog((services, loggerConfig) =>
        {
            SyncnetLoggerOption loggerOption = new();
            SyncnetTelemetryOption telemetryOption = new();

            if (LoggerAction != null) LoggerAction(loggerOption);
            if (TelemetryAction != null) TelemetryAction(telemetryOption); 

            loggerConfig
                .MinimumLevel.Is(loggerOption.MinimumLevel)
                .MinimumLevel.Override("Microsoft", loggerOption.Override)
                .Enrich.FromLogContext();
            
            if(loggerOption.EnableConsole) loggerConfig.WriteTo.Console();
            if(loggerOption.IncludeThreadId) loggerConfig.Enrich.WithThreadId();
            if (!string.IsNullOrEmpty(telemetryOption.Logging.Endpoint)) loggerConfig.WriteTo.OpenTelemetry(option =>
            {
                option.Endpoint = telemetryOption.Logging.Endpoint;
                option.Protocol = telemetryOption.Logging.Protocol == OpenTelemetry.Exporter.OtlpExportProtocol.Grpc ?
                    Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc :
                    Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
                option.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = builder.Environment.ApplicationName,
                };
            }, ignoreEnvironment: true);

        });
    }
    private static void ConfigureTelemetry(WebApplicationBuilder builder, Action<SyncnetTelemetryOption>? TelemetryAction = null)
    {
        SyncnetTelemetryOption telemetryOption = new();
        if (TelemetryAction != null) TelemetryAction(telemetryOption);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(Constants.Telemetry.MeterName);
                
                if(string.IsNullOrEmpty(telemetryOption.Metric.Endpoint))
                {
                    metrics.AddOtlpExporter();
                }
                else
                {
                    metrics.AddOtlpExporter(option =>
                    {
                        option.Endpoint = new Uri(telemetryOption.Metric.Endpoint);
                        option.Protocol = telemetryOption.Metric.Protocol;
                    });
                }
            })
            .WithTracing(trace =>
            {
                trace
                    .AddAspNetCoreInstrumentation( option =>
                    {
                        // /ws/gamesession shows whole life of the connection and it is meaningless.
                        option.Filter = httpContext =>
                        {
                            return httpContext.Request.Path != Constants.Endpoints.GameSessionWebSocket;
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(Constants.Telemetry.TraceSource);

                if(string.IsNullOrEmpty(telemetryOption.Trace.Endpoint))
                {
                    trace.AddOtlpExporter();
                }
                else
                {
                    trace.AddOtlpExporter(option =>
                    {
                        option.Endpoint = new Uri(telemetryOption.Trace.Endpoint);
                        option.Protocol = telemetryOption.Trace.Protocol;
                    });
                }
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

