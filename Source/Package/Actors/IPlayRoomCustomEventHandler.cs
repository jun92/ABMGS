namespace SyncnetPlatform.Actors;

public interface IPlayRoomCustomEventHandler<TPlayRoomMetaData> where TPlayRoomMetaData: IPlayRoomMetaData
{
    // Fill up with initial data to PlayRoom.
    IPlayRoomMetaData? InitializePlayRoomMetaData() => null;
    
    /// <summary>
    /// Fill _currentPlayRoomMetaData as a step of initializing play room.
    /// </summary>
    /// <param name="_currentPlayRoomMetaData"></param>
    /// <returns></returns>
    Task<TPlayRoomMetaData> OnPlayRoomInitializingAsync();
    
    Task OnPlayRoomDestroyingAsync();
    
    Task OnHandleCustomPacket(byte[] customPacket);
    
    TPlayRoomMetaData DeserializePlayRoomMetaData(byte[] roomMetaData);
    
    byte[] SerializePlayRoomMetaData(IPlayRoomMetaData playRoomMetaData);
}
