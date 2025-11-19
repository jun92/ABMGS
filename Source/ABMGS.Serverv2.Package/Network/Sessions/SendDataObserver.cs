using SyncnetPlatform.Interfaces.Network.Sessions;

namespace SyncnetPlatform.Network.Sessions;

public class SendDataObserver : ISendDataObserver
{
    private readonly SendQueueService _sendQueueService;
    private readonly Guid _playerId;
    public SendDataObserver(SendQueueService service, Guid PlayerId)
    {
        _sendQueueService = service;
        _playerId = PlayerId;
    }

    public async Task SendDataAsync(Guid playerId, byte[] data)
    {
        await _sendQueueService.SendDataAsync(playerId, data);
    }
}
