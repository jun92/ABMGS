namespace SyncnetPlatform.Actors;

public interface IPlayRoomCustomEventHandler<IPlayRoomMetaData>
{
    // Fill up with initial data to PlayRoom.
    IPlayRoomMetaData? InitializePlayRoomMetaData();
    
    /// <summary>
    /// Fill _currentPlayRoomMetaData as a step of initializing play room.
    /// </summary>
    /// <param name="_currentPlayRoomMetaData"></param>
    /// <returns></returns>
    Task<IPlayRoomMetaData> OnPlayRoomInitializingAsync();
    
    Task OnPlayRoomDestroyingAsync();
    
    Task OnHandleCustomPacket(byte[] customPacket);

    IPlayRoomMetaData DeserializePlayRoomMetaData(byte[] roomMetaData);
    
    byte[] SerializePlayRoomMetaData(IPlayRoomMetaData playRoomMetaData);

    Task OnPlayerAction(string actionName, byte[] actionParameter);

    Task AddPlayerToPlayRoom(Guid id, byte[] playerMetadata);

    Task OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter);
}
