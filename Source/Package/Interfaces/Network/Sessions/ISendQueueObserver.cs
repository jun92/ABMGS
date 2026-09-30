namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface ISendQueueObserver : IGrainObserver
{
    Task PushDataAsync(byte[] data);
}
