using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;

namespace SyncnetPlatform.Actors;

public class PlayerDataActor: Grain, IPlayerDataActor
{
    private readonly ILogger<PlayerDataActor> _logger;
    public PlayerDataActor(ILogger<PlayerDataActor> logger)
    {
        _logger = logger;
    }

    public Task<T> LoadExtendData<T>(Guid playerId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateExtendData<T>(Guid playerId, T extendData)
    {
        throw new NotImplementedException();
    }
}


