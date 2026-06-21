using OpenTelemetry.Exporter;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Extensions;
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




///================== initializing in old way
//var builder = WebApplication.CreateBuilder(args);

//builder.Configuration
//    .AddCommandLine(args)
//    .SetBasePath(Directory.GetCurrentDirectory())
//    .AddJsonFile("appsettings.json", false, true)
//    .AddJsonFile($"appsettings.{EnvironmentName}.json", true, true)
//    .AddEnvironmentVariables();


//// Define custom player data per games.
//builder.Services.AddTransient<IPlayerDataExtendCreater, MyGamePlayerDataExtendCreater>();
//builder.Services.AddTransient<IPlayerCustomBehavior, MyPlayerBehavior>();


//var IsSpecificTelemetryEndpoints = builder.Configuration.GetSection("ConnectionStrings:telemetry").Exists();


//Action<SyncnetTelemetryOption> telemetryConfigure = option =>
//{
//    option.Logging.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:Endpoint")!;
//    string protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:Protocol")!;
//    if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out var outProtocol))
//    {
//        option.Logging.Protocol = outProtocol;
//    }
//    option.Metric.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:Endpoint")!;
//    protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:Protocol")!;
//    if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
//    {
//        option.Metric.Protocol = outProtocol;
//    }
//    option.Trace.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:Endpoint")!;
//    protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:Protocol")!;
//    if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
//    {
//        option.Trace.Protocol = outProtocol;
//    }
//};
//IsSpecificTelemetryEndpoints = false;
//builder.AddSyncnetPlatformSilo(TelemetryAction:  IsSpecificTelemetryEndpoints ? telemetryConfigure : null);
////builder.AddSyncnetPlatformSilo();


//var host = builder.Build();
//host.SyncnetDbMigrate();
//await host.RunAsync();


//===========================================================================

