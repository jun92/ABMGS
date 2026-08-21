using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SyncnetPlatform.Utils.Telemetry;

public class SyncnetOutgoingGrainCallFilter : IOutgoingGrainCallFilter
{
    private readonly ILogger<SyncnetOutgoingGrainCallFilter> _logger;

    public SyncnetOutgoingGrainCallFilter(ILogger<SyncnetOutgoingGrainCallFilter> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (Activity.Current?.Id != null)
        {
            RequestContext.Set("traceparent", Activity.Current.Id);
        }
        await context.Invoke();
    }
}
