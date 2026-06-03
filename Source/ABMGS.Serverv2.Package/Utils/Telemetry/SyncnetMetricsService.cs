using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace SyncnetPlatform.Utils.Telemetry;

public class SyncnetMetricsService
{
    private readonly ILogger<SyncnetMetricsService> _logger;
    private readonly UpDownCounter<long> _activeConnectionCount;

    public SyncnetMetricsService(ILogger<SyncnetMetricsService> logger)
    {
        _logger = logger;
        _activeConnectionCount = SyncnetTelemetry.Meter.CreateUpDownCounter<long>(
            Constants.Telemetry.MeterName,
            "[connections]",
            "Number of active connections"
            );
    }

    public void AddConnection()
    {
        _activeConnectionCount.Add(1);
    }
    public void RemoveConnection()
    {
        _activeConnectionCount.Add(-1);
    }
}
