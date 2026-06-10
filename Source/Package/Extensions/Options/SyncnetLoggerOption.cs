using Serilog.Events;

namespace SyncnetPlatform.Extensions.Options;

public class SyncnetLoggerOption
{
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;
    public LogEventLevel Override { get; set; } = LogEventLevel.Warning;
    public bool EnableConsole { get; set; } = true;
    public bool IncludeThreadId { get; set; } = true;  
}


