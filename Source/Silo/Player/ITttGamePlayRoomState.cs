using SyncnetPlatform.Actors;

namespace Silo.Player;

public interface ITttGamePlayRoomState : IPlayRoomCustomState
{
    int CurrentInCount { get; }
    Guid WinnerPlayerId { get; }
    Guid GetPlayerIdInTurn();
    void TurnToNextPlayer();
    bool PutMarket(int x, int y, Guid playerId);
    bool IsGameOver();
    List<Guid> GetBroadcastTargets();
    void AddPlayer(Guid id, Dictionary<string, object?> extendData);
    void RemovePlayer(Guid id);
    bool SetPlayerReady(Guid playerId, bool readyState);
}
