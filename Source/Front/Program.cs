using Microsoft.Extensions.Configuration;
using SyncnetPlatform.Extensions.Options;
using OpenTelemetry.Exporter;
using SyncnetPlatform.ApplicationBuilder;


var builder = SyncnetApplicationBuilder.CreateFrontBuilder(args);

builder.ConfigureFront(options =>
{
    if (builder.Configuration.GetSection("ConnectionStrings:telemetry").Exists())
    // if(false)
    {
        Action<SyncnetTelemetryOption> telemetryConfigure = option =>
        {
            option.Logging.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:EndPoint")!;
            string protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:Protocol")!;
            if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out var outProtocol))
            {
                option.Logging.Protocol = outProtocol;
            }
            option.Metric.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:EndPoint")!;
            protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:Protocol")!;
            if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
            {
                option.Metric.Protocol = outProtocol;
            }
            option.Trace.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:EndPoint")!;
            protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:Protocol")!;
            if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
            {
                option.Trace.Protocol = outProtocol;
            }
        };
        options.TelemetryConfigure = telemetryConfigure; 
    }
});

var SyncnetFrontApp = builder.Build();

await SyncnetFrontApp.RunAsync();
