using SyncnetPlatform.Actors;

namespace Silo.Player;

public class MyPlayRoomMetaData : IPlayRoomMetaData
{
    public void Deserialize(byte[] serialized)
    {
    }

    public byte[] Serialize()
    {
        return Array.Empty<byte>();
    }
}

public class MyPlayRoomCustomBehavior : IPlayRoomCustomEventHandler<MyPlayRoomMetaData>
{
    public MyPlayRoomMetaData DeserializePlayRoomMetaData(byte[] roomMetaData)
    {
        throw new NotImplementedException();
    }

    public Task OnHandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        throw new NotImplementedException();
    }

    public Task<MyPlayRoomMetaData> OnPlayRoomInitializingAsync()
    {
        throw new NotImplementedException();
    }

    public byte[] SerializePlayRoomMetaData(IPlayRoomMetaData playRoomMetaData)
    {
        throw new NotImplementedException();
    }
}
