using SyncnetPlatform.Interfaces.Network.Sessions;

namespace SyncnetPlatform.Network.Sessions;

public class SendDataGrain : Grain, ISendDataGrain
{
    private ISendDataObserver? _sendDataObserver = null;
    public async Task Register(ISendDataObserver observer)
    {
        _sendDataObserver = observer;
    }

    public async Task Unregister()
    {
        _sendDataObserver = null;
    }

    public async Task Send(byte[] data)
    {
        if(_sendDataObserver is not null)
        {
            await _sendDataObserver.SendDataAsync(data);
        }
    }

    public Task IsValid()
    {
        return Task.FromResult(_sendDataObserver != null);
    }
}
