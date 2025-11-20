using SyncnetPlatform.Interfaces.Network.Sessions;

namespace SyncnetPlatform.Network.Sessions;

public class SendDataObserver : ISendDataObserver
{
    private readonly GameSessionService _gameSessionService;
    public SendDataObserver(GameSessionService service)
    {
        _gameSessionService = service;
    }

    public async Task SendDataAsync(byte[] data)
    {
        await _gameSessionService.SendDataAsync(data);
    }
}
