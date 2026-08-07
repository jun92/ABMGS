
namespace SyncnetPlatform.Actors;

public interface IPlayerCustomBehavior
{
    Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null);
    Task<bool> OnLogoutAsync(PlayerState playerData, CancellationToken? cancellationToken = null);
    Task HandleCustomPacket(byte[] customPacket);

    // return Array.Empty<byte> if no available serializer or data. 
    byte[] SerializePlayerMetadata(Dictionary<string, object?> playerState, CancellationToken? cancellationToken = null);
    void UpdatePlayerExtendDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState);


    // When the play join a playroom
    void OnJoinPlayRoom<TPlayRoomMetaData>(PlayerState playerState, Guid playRoomId, bool isOwner, TPlayRoomMetaData? roomMetaData = default) where TPlayRoomMetaData : IPlayRoomCustomState;
}


