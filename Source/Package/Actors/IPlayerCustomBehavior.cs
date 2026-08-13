
namespace SyncnetPlatform.Actors;

public interface IPlayerCustomBehavior
{
    Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null);
    Task<bool> OnLogoutAsync(PlayerState playerData, CancellationToken? cancellationToken = null);
    Task HandleCustomPacket(byte[] customPacket);

    // return Array.Empty<byte> if no available serializer or data. 
    byte[] SerializePlayerExtendData(Dictionary<string, object?> playerState, CancellationToken? cancellationToken = null);
    Dictionary<string, object?> DeserializePlayerExtendData(byte[] data);
    void UpdatePlayerExtendDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState);
    // When the play join a playroom
    void OnJoinPlayRoom(PlayerState playerState, Guid playRoomId, bool isOwner, byte[] roomState);
}


