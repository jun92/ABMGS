using OpenTelemetry.Exporter;
using System.ComponentModel.DataAnnotations;

namespace SyncnetPlatform.Extensions.Options;

public class SyncnetTelemetryOption
{
    public required TelemetryEndpoint Logging { get; set; } = new();
    public required TelemetryEndpoint Trace { get; set; } = new();
    public required TelemetryEndpoint Metric { get; set; } = new();
}

public class TelemetryEndpoint
{
    public string Endpoint { get; set; } = String.Empty;
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;
}



