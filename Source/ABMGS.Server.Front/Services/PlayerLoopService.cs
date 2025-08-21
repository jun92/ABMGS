using System.Net.WebSockets;
using ABMGS.Server.Front;
using ABMGS.Server.Front.Interfaces.Players;
using ABMGS.Server.Front.Services.Player;


/// <summary> 
/// 루프를 돌면서 미들웨어가 종료되지 않게 유지하는 서비스
/// </summary>
public class PlayerLoopService
{
    private readonly ILogger<PlayerLoopService> _logger;
    private Player _player;
    private MemoryStream _readStream = new MemoryStream();
    private WebSocket _webSocket;
    private readonly IPlayerFactory _playerFactory;

    public PlayerLoopService(ILogger<PlayerLoopService> logger, IPlayerFactory playerFactory)
    {
        _logger = logger;
        _playerFactory = playerFactory;
    }

    /// <summary>
    /// 지속적으로 루프를 돌면서 네트워크 receive/send를 처리합니다.
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public async Task StartSessionLoop(WebSocket webSocket, Guid playerId)
    {
        IPlayer Player = _playerFactory.CreatePlayer(playerId);

        // Session이 종료될때 fire되는 TaskCompletionSource
        CancellationTokenSource sessionEndNotification = new();
        
        
        // RecevieWait중에 뭔가를 보내야할 때, Receive를 취소하기 위한 CancellationTokenSource
        CancellationTokenSource cancelReceiveDueToSomethingToSend = new();
        cancelReceiveDueToSomethingToSend.Token.Register(Player.FlushSendBuffer);


        CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            sessionEndNotification.Token,
            cancelReceiveDueToSomethingToSend.Token
        );


        //while (!sessionEndTokenSource.Token.IsCancellationRequested)
        //{
        //    var buffer = new byte[ABMGSConfig.MaxWebSocketMessageSize];
        //    var timeoutToken = CreateTimeoutToken(TimeSpan.FromMilliseconds(ABMGSConfig.RequestTimeoutMilliseconds));
        //    await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutToken);
        //}

        await WaitReceiveAMessageOrDiscardDueToSendBack(cancellationTokenSource.Token);
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
            catch (OperationCanceledException e)
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
