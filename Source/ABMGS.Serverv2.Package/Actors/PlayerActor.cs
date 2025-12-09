using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Repositories;

namespace SyncnetPlatform.Actors;

public interface IPlayerBehavior
{

}

public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;
    private readonly IPlayerModelRepository _repository;

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Echo(int seq)
    {
        return Task.CompletedTask;
    }

}


