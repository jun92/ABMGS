using OpenTelemetry.Exporter;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Extensions.Options;

string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddCommandLine(args)
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

bool UseMyCustomDb = false;

if( UseMyCustomDb )
{
    // disable temporary.
    //builder.AddSyncnetPlatformSilo(optionsBulider =>
    //{
    //    optionsBulider.UseBuiltinDbContext = false;
    //    optionsBulider.RegisterDbContext<SyncnetDbContextExtend>(builder);
    //});
}
else
{
    var IsSpecificTelemetryEndpoints = builder.Configuration.GetSection("ConnectionStrings:telemetry").Exists();


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
    builder.AddSyncnetPlatformSilo(TelemetryAction:  IsSpecificTelemetryEndpoints ? telemetryConfigure : null);
    //builder.AddSyncnetPlatformSilo();
}

var host = builder.Build();
if(UseMyCustomDb)
{
    host.SyncnetDbMigrate<SyncnetDbContextExtend>();
}
else
{
    host.SyncnetDbMigrate();
}
await host.RunAsync();


