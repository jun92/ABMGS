using SyncnetPlatform.Actors;

namespace Silo.Player;

public class MyPlayRoomCustomBehavior : IPlayRoomCustomEventHandler
{
    public Task OnHandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        throw new NotImplementedException();
    }

    public Task OnPlayRoomInitializingAsync(byte[]? roomMetaData)
    {

        throw new NotImplementedException();
    }
}
