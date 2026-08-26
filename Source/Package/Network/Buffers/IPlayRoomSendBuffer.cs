
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncnetPlatform.Network.Buffers;


public interface IPlayRoomSendBuffer
{
    List<Guid> GetPlayersHavePendingData();
    void BroadcastToAll(byte[] buffer);
    void BroadcastFiltered(List<Guid> playerIds, byte[] buffer);
}

public class PlayRoomSendBuffer : IPlayRoomSendBuffer
{
    private readonly Dictionary<Guid, Queue<byte[]>> _sendBuffer = new();
    private readonly Queue<byte[]> _sendBufferToAll = new();

    private void PushBuffer(Guid playerId, byte[] buffer)
    {
        if(!_sendBuffer.TryGetValue(playerId, out Queue<byte[]>? queue))
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

    private byte[]? PopBuffer(Guid playerId)
    {
        return _sendBuffer[playerId].Count == 0 ? null : _sendBuffer[playerId].Dequeue();
    }

    public byte[]? GetBufferForAllPlayers()
    {
        return _sendBufferToAll.Count == 0 ? null : _sendBufferToAll.Dequeue();
    }

    public void BroadcastToAll(byte[] buffer)
    {
        _sendBufferToAll.Enqueue(buffer);
    }


    public void BroadcastFiltered(List<Guid> playerIds, byte[] buffer)
    {
        playerIds.ForEach(p => PushBuffer(p, buffer));
    }

}
