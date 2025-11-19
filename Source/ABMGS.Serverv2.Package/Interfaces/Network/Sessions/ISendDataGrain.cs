namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface ISendDataGrain : IGrainWithGuidKey
{
    Task Register(ISendDataObserver observer);
    Task Unregister();
    Task Send(Guid playerId, byte[] data);
}
