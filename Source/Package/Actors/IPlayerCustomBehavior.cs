
namespace SyncnetPlatform.Actors;

public interface IPlayerCustomBehavior
{
    Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null);
    Task<bool> OnLogoutAsync(CancellationToken? cancellationToken = null);

    void UpdatePlayerExtendDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState);
    void OnJoinPlayRoom(PlayerState playerState, Guid playRoomId, bool isOwner, byte[]? roomState);
    
    IPlayerExtendData GetPlayerCustomState();
}

public interface IPlayerExtendData
{
    void Initialize(IReadOnlyDictionary<string, object?> state);
    byte[] Serialize(IReadOnlyDictionary<string, object?> playerState);
    Dictionary<string, object?> Deserialize(byte[] data);
    Dictionary<string, object?> Deserialize();
}


