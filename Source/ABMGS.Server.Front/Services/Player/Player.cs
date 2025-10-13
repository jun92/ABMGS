using ABMGS.Server.Front.Interfaces.Networks;
using ABMGS.Server.Front.Interfaces.Players;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace ABMGS.Server.Front.Services.Player;

/// <summary> 
/// 플레이어 관련 데이타 저장
/// SendQueue도 소유
/// Receive용 버퍼도 소유
/// </summary>
/// <param name="_id"></param>
/// <param name="webSocket"></param>
public class Player : IPlayer
{
    private Guid id;
    private readonly INetworkBuffer _buffer = new NetworkBuffer();

    public Player(Guid id)
    {
        this.id = id;
        
    }

    public Guid Id() => id;

    public INetworkBuffer GetBuffer()
    {
        throw new NotImplementedException();
    }

    public void FlushSendBuffer()
    {
        GetBuffer().PopSendData();

    }

}
