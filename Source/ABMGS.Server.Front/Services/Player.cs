using System.Net.WebSockets;

namespace ABMGS.Server.Front.Services;

/// <summary> 
/// 플레이어 관련 데이타 저장
/// SendQueue도 소유
/// Receive용 버퍼도 소유
/// </summary>
/// <param name="_id"></param>
/// <param name="webSocket"></param>
public class Player(Guid _id)
{
    private Guid id = _id;
    
    public async Task SendAsync(byte[] data, CancellationToken cancellationToken)
    {
        if (_webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not open.");
        }
        var segment = new ArraySegment<byte>(data);
        await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
    }
}
