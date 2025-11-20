using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;

namespace SyncnetPlatform.Actors;

public class PlayerInventoryActor : Grain, IPlayerInventoryActor
{
    private readonly ILogger<PlayerInventoryActor> _logger;
    public PlayerInventoryActor(ILogger<PlayerInventoryActor> logger)
    {
        _logger = logger;
    }

    public void AddItem(Guid id)
    {
        throw new NotImplementedException();
    }

    public void DeleteItem(Guid id)
    {
        throw new NotImplementedException();
    }
}


