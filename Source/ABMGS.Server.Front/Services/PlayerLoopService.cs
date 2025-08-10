using System.Net.WebSockets;
using ABMGS.Server.Front;

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
            var buffer = new byte[Config.MaxWebSocketMessageSize];
            var timeoutToken = CreateTimeoutToken(TimeSpan.FromMilliseconds(Config.RequestTimeoutMilliseconds));
            await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutToken);
        }
    }
    private CancellationToken CreateTimeoutToken(TimeSpan howLong)
    {
        CancellationTokenSource timeoutCancellationToken = new CancellationTokenSource();
        timeoutCancellationToken.CancelAfter(howLong);
        return timeoutCancellationToken.Token;
    }

    private static CancellationTokenSource GetTimeoutToken()
    {
        return new CancellationTokenSource();
    }

    protected bool CanContinue() => _webSocket != null && _playerId != null;
}
