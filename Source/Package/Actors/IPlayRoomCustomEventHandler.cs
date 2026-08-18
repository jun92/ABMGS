namespace SyncnetPlatform.Actors;

public interface IPlayRoomCustomEventHandler
{
    Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync();
    
    Task OnPlayRoomDestroyingAsync();
    
    IPlayRoomCustomState DeserializePlayRoomState(byte[] roomMetaData);
    byte[]  SerializePlayRoomState(IPlayRoomCustomState playRoomCustomState);

    Task AddPlayerToPlayRoom(Guid id, byte[] playerCustomState);

    Task<(Dictionary<Guid, byte[]>, byte[]?)> OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter);
    Task OnTimer(float delta);
}
