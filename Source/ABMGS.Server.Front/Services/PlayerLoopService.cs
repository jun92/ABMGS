using System.Net.WebSockets;


public class PlayerLoopService
{
    private readonly ILogger<PlayerLoopService> _logger;
    private WebSocket? _webSocket;
    private Guid? _playerId;

    public PlayerLoopService(ILogger<PlayerLoopService> logger)
    {
        _logger = logger;
    }

    public async Task StartSessionLoop(WebSocket webSocket, Guid playerId)
    {
        CancellationToken sessionEndToken = new CancellationToken();
        _webSocket = webSocket;
        _playerId = playerId;

        while (!sessionEndToken.IsCancellationRequested)
        {
            if (!CanContinue())
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                continue;
            }

            var buffer = new byte[1024 * 4];

            CancellationTokenSource timeoutCancellationToken = new CancellationTokenSource();
            timeoutCancellationToken.CancelAfter(TimeSpan.FromMilliseconds(50));

            await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutCancellationToken.Token);


        }
    }

    private static CancellationTokenSource GetTimeoutToken()
    {
        return new CancellationTokenSource();
    }

    protected bool CanContinue() => _webSocket != null && _playerId != null;
}
