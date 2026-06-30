using OpenTelemetry.Exporter;
using SyncnetPlatform.Actors;
using SyncnetPlatform.ApplicationBuilder;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Extensions.Options;

string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");


var builder = SyncnetApplicationBuilder.CreateActorBuilder(args);
builder.ConfigureActor(option =>
{
    option.UsePlayerCustomBehavior<MyPlayerBehavior>();
    option.UsePlayerDataExtendCreator<MyGamePlayerDataExtendCreater>();
    option.AutoMigrateDatabase = true;

    if( builder.Configuration.GetSection("ConnectionStrings:telemetry").Exists())
    {
        Action<SyncnetTelemetryOption> telemetryConfigure = option =>
        {
            option.Logging.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:Endpoint")!;
            string protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:Protocol")!;
            if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out var outProtocol))
            {
                option.Logging.Protocol = outProtocol;
            }
            option.Metric.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:Endpoint")!;
            protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:Protocol")!;
            if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
            {
                option.Metric.Protocol = outProtocol;
            }
            option.Trace.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:Endpoint")!;
            protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:Protocol")!;
            if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
            {
                option.Trace.Protocol = outProtocol;
            }
        };
        option.TelemetryConfigure = telemetryConfigure;
    }
});

var SyncnetActorApp = builder.Build();
await SyncnetActorApp.RunAsync();
