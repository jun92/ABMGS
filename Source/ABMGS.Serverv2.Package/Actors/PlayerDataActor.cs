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
}


