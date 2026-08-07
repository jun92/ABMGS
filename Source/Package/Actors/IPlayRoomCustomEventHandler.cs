namespace SyncnetPlatform.Actors;

public interface IPlayRoomCustomEventHandler
{
    Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync();
    
    Task OnPlayRoomDestroyingAsync();
    
    Task OnHandleCustomPacket(byte[] customPacket);

    IPlayRoomCustomState DeserializePlayRoomMetaData(byte[] roomMetaData);

    //Task OnPlayerAction(string actionName, byte[] actionParameter);

    Task AddPlayerToPlayRoom(Guid id, byte[] playerMetadata);

    Task<(Dictionary<Guid, byte[]>, byte[])> OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter);
    Task OnTimer(float delta);
}
