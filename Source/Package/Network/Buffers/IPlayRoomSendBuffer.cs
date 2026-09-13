
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncnetPlatform.Network.Buffers;


public interface IPlayRoomSendBuffer
{
    List<Guid> GetPlayersHavePendingData();
    void BroadcastToAll(string actionType, byte[] buffer);
    void BroadcastFiltered(List<Guid> playerIds, string actionType, byte[] buffer);
    
    (string?, byte[]?) GetBufferForAllPlayers();
    (string?, byte[]?) PopBuffer(Guid playerId);
}

public class PlayRoomSendBuffer : IPlayRoomSendBuffer
{
    private readonly Dictionary<Guid, Queue<(string, byte[])>> _sendBuffer = new();
    private readonly Queue<(string, byte[])> _sendBufferToAll = new();

    private void PushBuffer(Guid playerId, string actionType, byte[] buffer)
    {
        if(!_sendBuffer.TryGetValue(playerId, out Queue<(string, byte[])>? queue))
        {
            _sendBuffer[playerId] = new Queue<(string, byte[])>();
        }
        _sendBuffer[playerId].Enqueue((actionType, buffer));
    }

    public List<Guid> GetPlayersHavePendingData() =>
        _sendBuffer
            .Where(k => k.Value.Count != 0)
            .Select(s => s.Key)
            .ToList();

    public (string?, byte[]?) PopBuffer(Guid playerId)
    {
        return _sendBuffer[playerId].Count == 0 ? (null, null) : _sendBuffer[playerId].Dequeue();
    }

    public (string?, byte[]?) GetBufferForAllPlayers()
    {
        return _sendBufferToAll.Count == 0 ? (null, null) : _sendBufferToAll.Dequeue();
    }

    public void BroadcastToAll(string actionType, byte[] parameters)
    {
        _sendBufferToAll.Enqueue((actionType, parameters));
    }


    public void BroadcastFiltered(List<Guid> playerIds, string actionType, byte[] parameters)
    {
        playerIds.ForEach(p => PushBuffer(p, actionType, parameters));
    }

}
