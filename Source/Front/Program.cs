using Microsoft.Extensions.Configuration;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Extensions.Options;
using OpenTelemetry.Exporter;


var builder = SyncnetApplicationBuilder.CreateFrontBuilder(args);

builder.ConfigureFront(options =>
{
    if (builder.Configuration.GetSection("ConnectionStrings:telemetry").Exists())
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

//var builder = WebApplication.CreateBuilder(args);


//// if want to use external telemetry services, for using default aspire dashboard, should be null.
//var IsSpecificTelemetryEndpoints = builder.Configuration.GetSection("ConnectionStrings:telemetry").Exists();

//Action<SyncnetTelemetryOption> telemetryConfigure = option =>
//{
//    option.Logging.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:EndPoint")!;
//    string protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:logging:Protocol")!;
//    if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out var outProtocol))
//    {
//        option.Logging.Protocol = outProtocol;
//    }
//    option.Metric.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:EndPoint")!;
//    protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:metric:Protocol")!;
//    if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
//    {
//        option.Metric.Protocol = outProtocol;
//    }
//    option.Trace.Endpoint = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:EndPoint")!;
//    protocol = builder.Configuration.GetValue<string>("ConnectionStrings:telemetry:trace:Protocol")!;
//    if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: false, out outProtocol))
//    {
//        option.Trace.Protocol = outProtocol;
//    }
//};
//IsSpecificTelemetryEndpoints = false;
//builder.AddSyncnetPlatformClient(TelemetryAction: IsSpecificTelemetryEndpoints ? telemetryConfigure: null);

//builder.Services.AddControllers();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseRouting();
//app.UseAuthentication();
//app.UseAuthorization();
//app.UseFrontendSyncnetPlatform();

//app.MapControllers();
////app.UseEndpoints(endpoints =>
////{
////    endpoints.MapControllers();
////});

//await app.RunAsync();



