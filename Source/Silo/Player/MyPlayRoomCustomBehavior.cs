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

    public Task OnPlayRoomInitializingAsync(PlayRoomState _currentRoomState, byte[]? roomMetaData)
    {

        throw new NotImplementedException();
    }
}
