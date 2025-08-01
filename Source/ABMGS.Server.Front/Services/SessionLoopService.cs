using System.Net.WebSockets;


public class SessionLoopService : BackgroundService
{
    private readonly ILogger<SessionLoopService> _logger;
    private WebSocket? _webSocket;

    public SessionLoopService(ILogger<SessionLoopService> logger)
    {
        _logger = logger;
    }

    public void StartSessionLoop(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (_webSocket)
        {

        }
        throw new NotImplementedException();
    }
}
