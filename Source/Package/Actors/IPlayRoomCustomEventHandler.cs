namespace SyncnetPlatform.Actors;

public interface IPlayRoomCustomEventHandler
{
    Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync();
    
    Task OnPlayRoomDestroyingAsync();
    
    Task OnHandleCustomPacket(byte[] customPacket);

    IPlayRoomCustomState DeserializePlayRoomMetaData(byte[] roomMetaData);
    
    byte[] SerializePlayRoomMetaData(IPlayRoomCustomState playRoomMetaData);

    Task OnPlayerAction(string actionName, byte[] actionParameter);

    Task AddPlayerToPlayRoom(Guid id, byte[] playerMetadata);

    Task OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter);
    Task OnTimer(float delta);
}
