using SyncnetPlatform.Interfaces.Network.Sessions;

namespace SyncnetPlatform.Network.Sessions;

public class SendQueueActor : Grain, ISendQueueActor
{
    private ISendQueueObserver? _sendQueueObserver = null;
    public async Task Register(ISendQueueObserver observer)
    {
        _sendQueueObserver = observer;
    }

    public async Task Unregister()
    {
        _sendQueueObserver = null;
    }

    public async Task Push(byte[] data)
    {
        if(_sendQueueObserver is not null)
        {
            await _sendQueueObserver.PushDataAsync(data);
        }
    }

    public Task IsValid()
    {
        return Task.FromResult(_sendQueueObserver != null);
    }
}
