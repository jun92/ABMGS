using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SyncnetPlatform.Utils.Telemetry;

public static class SyncnetTelemetry
{
    public static readonly ActivitySource TraceSource = new(Constants.Telemetry.TraceSource);
    public static readonly Meter Meter = new(Constants.Telemetry.MeterName);
}
