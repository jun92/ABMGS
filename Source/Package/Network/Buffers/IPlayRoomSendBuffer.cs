
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncnetPlatform.Network.Buffers;


public interface IPlayRoomSendBuffer
{
    void PushBuffer(Guid  playerId, byte[] buffer);
    List<Guid> GetPlayersHavePendingData();
    byte[]? PopBuffer(Guid playerId);
}

public class PlayRoomSendBuffer : IPlayRoomSendBuffer
{
    private readonly Dictionary<Guid, Queue<byte[]>> _sendBuffer = new();

    public void PushBuffer(Guid playerId, byte[] buffer)
    {
        if (!_sendBuffer.ContainsKey(playerId))
        {
            _sendBuffer[playerId] = new Queue<byte[]>();
        }
        _sendBuffer[playerId].Enqueue(buffer);
    }

    public List<Guid> GetPlayersHavePendingData() =>
        _sendBuffer
            .Where(k => k.Value.Count != 0)
            .Select(s => s.Key)
            .ToList();

    public byte[]? PopBuffer(Guid playerId)
    {
        return _sendBuffer[playerId].Count == 0 ? null : _sendBuffer[playerId].Dequeue();
    }
}
