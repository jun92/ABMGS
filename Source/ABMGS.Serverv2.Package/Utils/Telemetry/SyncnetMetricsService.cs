using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace SyncnetPlatform.Utils.Telemetry;

public class SyncnetMetricsService
{
    private readonly ILogger<SyncnetMetricsService> _logger;
    private readonly UpDownCounter<long> _activeConnectionCount;

    // Orleans Grain 호출 메트릭
    private readonly Counter<long> _grainCalls;
    private readonly Histogram<double> _grainCallDuration;
    // 패킷 처리 메트릭
    private readonly Counter<long> _packetsProcessed;
    private readonly Histogram<double> _packetProcessingDuration;

    public SyncnetMetricsService(ILogger<SyncnetMetricsService> logger)
    {
        _logger = logger;
        _activeConnectionCount = SyncnetTelemetry.Meter.CreateUpDownCounter<long>(
            Constants.Telemetry.ConnectionMeterName,
            "{connections}",
            "Number of active connections"
            );
        _grainCalls = SyncnetTelemetry.Meter.CreateCounter<long>(
            "syncnet.grain.calls.total",
            "{calls}",
            "Total number of Orleans grain calls"
        );
        _grainCallDuration = SyncnetTelemetry.Meter.CreateHistogram<double>(
            "syncnet.grain.calls.duration",
            "ms",
            "Duration of Orleans grain calls in milliseconds"
        );
        _packetsProcessed = SyncnetTelemetry.Meter.CreateCounter<long>(
            "syncnet.packets.processed",
            "{packets}",
            "Total number of processed network packets"
        );
        _packetProcessingDuration = SyncnetTelemetry.Meter.CreateHistogram<double>(
            "syncnet.packets.duration",
            "ms",
            "Duration of network packet processing in milliseconds"
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
    public void RecordGrainCall(string grainType, string methodName, double durationMs)
    {
        var tags = new TagList
        {
            { "orleans.grain.type", grainType },
            { "orleans.grain.method", methodName }
        };
        _grainCalls.Add(1, tags);
        _grainCallDuration.Record(durationMs, tags);
    }
    public void RecordPacketProcessed(string packetType, double durationMs, string status)
    {
        var tags = new TagList
        {
            { "packet.type", packetType },
            { "status", status }
        };
        _packetsProcessed.Add(1, tags);
        _packetProcessingDuration.Record(durationMs, tags);
    }
}
