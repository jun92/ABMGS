namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface ISendDataGrain : IGrainWithGuidKey
{
    Task Register(ISendDataObserver observer);
    Task Send(Guid playerId, byte[] data);
}
