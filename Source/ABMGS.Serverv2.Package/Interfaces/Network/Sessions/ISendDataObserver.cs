namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface ISendDataObserver : IGrainObserver
{
    Task SendDataAsync(Guid playerId, byte[] data);
}
