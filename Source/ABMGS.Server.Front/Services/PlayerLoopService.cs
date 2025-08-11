using System.Net.WebSockets;
using ABMGS.Server.Front;
using ABMGS.Server.Front.Services;


/// <summary> 
/// 기본 아이더 1차 : 소켓 아이오를 담당하고 플레이어 객체에 데이타를 저장해준다.
/// </summary>
public class PlayerLoopService
{
    private readonly ILogger<PlayerLoopService> _logger;
    private Player _player;
    private MemoryStream _readStream = new MemoryStream();
    private WebSocket _webSocket;

    public PlayerLoopService(ILogger<PlayerLoopService> logger)
    {
        _logger = logger;
        _player = new Player(Guid.NewGuid());
    }

    public async Task StartSessionLoop(WebSocket webSocket, Guid playerId)
    {
        CancellationTokenSource sessionEndTokenSource = new CancellationTokenSource();
        _webSocket = webSocket;

        //while (!sessionEndTokenSource.Token.IsCancellationRequested)
        //{
        //    var buffer = new byte[ABMGSConfig.MaxWebSocketMessageSize];
        //    var timeoutToken = CreateTimeoutToken(TimeSpan.FromMilliseconds(ABMGSConfig.RequestTimeoutMilliseconds));
        //    await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutToken);
        //}

        await WaitReceiveAMessageOrDiscardDueToSendBack(sessionEndTokenSource.Token);
    }
    public async Task<ArraySegment<Byte>> WaitReceiveAMessageOrDiscardDueToSendBack(CancellationToken cancellationToken)
    {
        if (_webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not open.");
        }

        var segment = new ArraySegment<byte>(new byte[ABMGSConfig.MaxWebSocketMessageSize]);

        WebSocketReceiveResult receiveResult;
        do
        {
            try
            {
                receiveResult = await _webSocket.ReceiveAsync(segment, cancellationToken);
            }
            catch(OperationCanceledException e)
            {
                return null;
            }
            if (segment.Array != null && segment.Count != 0)
            {
                _readStream.Write(segment.Array, segment.Offset, receiveResult.Count);
            }
        } while (!receiveResult.EndOfMessage);

        return new ArraySegment<byte>(_readStream.GetBuffer(), 0, (int)_readStream.Length);
    }
    private CancellationToken CreateTimeoutToken(TimeSpan howLong)
    {
        CancellationTokenSource timeoutCancellationToken = new CancellationTokenSource();
        timeoutCancellationToken.CancelAfter(howLong);
        return timeoutCancellationToken.Token;
    }

 
}
