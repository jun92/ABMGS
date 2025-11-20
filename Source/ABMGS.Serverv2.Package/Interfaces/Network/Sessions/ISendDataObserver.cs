namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface ISendDataObserver : IGrainObserver
{
    Task SendDataAsync(byte[] data);
}
