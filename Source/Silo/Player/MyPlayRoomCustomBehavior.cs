using SyncnetPlatform.Actors;

namespace Silo.Player;

public class MyPlayRoomMetaData : IPlayRoomMetaData
{


}

public class MyPlayRoomCustomBehavior : IPlayRoomCustomEventHandler<MyPlayRoomMetaData>
{
    public Task OnHandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        throw new NotImplementedException();
    }

    public MyPlayRoomMetaData DeserializePlayRoomMetaData(byte[] roomMetaData)
    {
        // FlatBuffer Packet to MyPlayRoomMetaData

        throw new NotImplementedException();
    }

    public Task OnPlayRoomInitializingAsync(IPlayRoomMetaData? _currentPlayRoomMetaData, MyPlayRoomMetaData roomMetaData)
    {
        //Build Playroom metadata at initialization step.
        throw new NotImplementedException();
    }

    public byte[] SerializePlayRoomMetaData(IPlayRoomMetaData playRoomMetaData)
    {
        throw new NotImplementedException();
    }
}
