using ABMGS.Server.Front.Interfaces.Networks;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ABMGS.Server.Front.Services;

public interface IPlayer
{
    public Guid Id();
    public INetworkBuffer GetBuffer();
}

public interface IPlayerFactory
{
    public IPlayer CreatePlayer(Guid id);
}


public interface IParser
{
    public T Parse<T>(byte[] data) where T : IPacket;
}

public class JsonParser : IParser
{
    public T Parse<T>(byte[] data) where T : IPacket
    {
        throw new NotImplementedException();
    }
}

public interface IPacket
{

}

public class BasicPlayerFactory : IPlayerFactory
{
    public IPlayer CreatePlayer(Guid id)
    {
        return new Player(id);
    }
}

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
    /// <summary>
    /// 세션 종료가 되면 fire되는 TaskCompletionSource
    /// </summary>
    private readonly TaskCompletionSource _sessionTerminationSource = new TaskCompletionSource();

    public Player(Guid id)
    {
        this.id = id;
    }

    public Guid Id() => id;

    public INetworkBuffer GetBuffer()
    {
        throw new NotImplementedException();
    }
}

public class NetworkBuffer : INetworkBuffer
{
    private readonly ConcurrentQueue<byte[]> _sendQueue = new();
    public NetworkBuffer()
    {
        // Initialize buffer or any other necessary setup
    }

    public void AddReceiveData(byte[] data)
    {
        throw new NotImplementedException();
    }

    public void EnqueueSendData(byte[] data)
    {
        _sendQueue.Enqueue(data);
    }

    public byte[] GetReceiveData()
    {
        throw new NotImplementedException();
    }
    public byte[] PopSendData()
    {
        throw new NotImplementedException();
    }

    public void PushSendData(byte[] data)
    {
        throw new NotImplementedException();
    }

}
