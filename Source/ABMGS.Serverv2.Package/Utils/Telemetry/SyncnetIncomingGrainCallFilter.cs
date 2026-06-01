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
    private static readonly ConcurrentDictionary<MethodInfo, CallMetadata> _callMetadata = new();

    public SyncnetIncomingGrainCallFilter(ILogger<SyncnetIncomingGrainCallFilter> logger)
    {
        _logger = logger;
    }



    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (context.Grain.GetType().Namespace == null || !context.Grain.GetType().Namespace.StartsWith("SyncnetPlatform"))
        {
            await context.Invoke();
            return;
        }
        var metadata = _callMetadata.GetOrAdd(context.InterfaceMethod, method =>
        {
            return new CallMetadata(method, context.Grain.GetType());
        });
        using var activity = SyncnetTelemetry.Trace.StartActivity(metadata.ActivityName, ActivityKind.Server);
        if (activity != null)
        {
            activity.SetTag("orleans.grain.type", metadata.GrainType);
            activity.SetTag("orleans.grain.method", metadata.MethodName);
            activity.SetTag("orleans.grain.id", context.TargetContext.GrainId.ToString());
        }

        await context.Invoke();
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