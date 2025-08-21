using ABMGS.Server.Front.Interfaces.Networks;
using System.Collections.Concurrent;

namespace ABMGS.Server.Front.Services;

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
