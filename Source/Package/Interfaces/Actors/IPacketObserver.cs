namespace SyncnetPlatform.Interfaces.Actors;

public interface IPacketObserver : IGrainObserver
{
    public Task NewPacketArrived();
}


