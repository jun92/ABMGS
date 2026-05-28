using SyncnetPlatform.Extensions;

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
    //builder.AddSyncnetPlatformSilo(optionsBulider =>
    //{
    //    optionsBulider.UseBuiltinDbContext = false;
    //    optionsBulider.RegisterDbContext<SyncnetDbContextExtend>(builder);
    //});
}
else
{
    builder.AddSyncnetPlatformSilo(TelemetryAction: option =>
    {
        option.Logging.Endpoint = "http://loki.syncnet.dev:4317";
        option.Logging.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

        option.Trace.Endpoint = "http://tempo.syncnet.dev:4317";
        option.Trace.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

        option.Metric.Endpoint = "http://prometheus.syncnet.dev:9090/api/v1/otlp/v1/metrics";
        option.Metric.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
    });
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


