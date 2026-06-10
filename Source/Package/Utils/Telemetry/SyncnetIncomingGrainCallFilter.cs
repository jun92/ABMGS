using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SyncnetPlatform.Utils.Telemetry;

public class SyncnetIncomingGrainCallFilter : IIncomingGrainCallFilter
{
    private readonly ILogger<SyncnetIncomingGrainCallFilter> _logger;
    //private static readonly ConcurrentDictionary<MethodInfo, CallMetadata> _callMetadata = new();
    private static readonly ConcurrentDictionary<(MethodInfo, Type), CallMetadata> _callMetadata = new();
    private readonly SyncnetMetricsService _metricsService;

    public SyncnetIncomingGrainCallFilter(ILogger<SyncnetIncomingGrainCallFilter> logger, SyncnetMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }



    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var grainNamespace = context.Grain.GetType().Namespace;
        if (grainNamespace == null || !grainNamespace.StartsWith("SyncnetPlatform"))
        {
            await context.Invoke();
            return;
        }
        var metadata = _callMetadata.GetOrAdd((context.InterfaceMethod, context.Grain.GetType()), key =>
        {
            return new CallMetadata(key.Item1, key.Item2);
        });

        ActivityContext parentContext = default;
        if (RequestContext.Get("traceparent") is string traceparent && ActivityContext.TryParse(traceparent, null, out var parsedContext))
        {
            parentContext = parsedContext;

        }
        using var activity = SyncnetTelemetry.Trace.StartActivity(metadata.ActivityName, ActivityKind.Server, parentContext);
        if (activity != null)
        {
            activity.SetTag("orleans.grain.type", metadata.GrainType);
            activity.SetTag("orleans.grain.method", metadata.MethodName);
            activity.SetTag("orleans.grain.id", context.TargetContext.GrainId.ToString());
        }
        long startTime = Stopwatch.GetTimestamp();
        try
        {
            await context.Invoke();
        }
        finally
        {
            double elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            _metricsService.RecordGrainCall(metadata.GrainType, metadata.MethodName, elapsedMs);
        }
    }
}

internal sealed class CallMetadata
{
    public string ActivityName { get; }
    public string GrainType { get; }
    public string MethodName { get; }
    public CallMetadata(MethodInfo methodInfo, Type concreteGrainType)
    {
        GrainType = concreteGrainType.Name;
        MethodName = methodInfo.Name;
        ActivityName = $"Grain.{GrainType}.{MethodName}";
    }
}