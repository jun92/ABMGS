using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Repositories;

namespace SyncnetPlatform.Actors;

public interface IPlayerBehavior
{

}

public class PlayerActor : Grain, IPlayerActor
{
    private int _dbid;
    private readonly ILogger<PlayerActor> _logger;
    private readonly IPlayerModelRepository _playerModelRepository;

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepository playerModelRepository
        )
    {
        _logger = logger;
        _playerModelRepository = playerModelRepository;
        
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Guid PlayerId = GrainContext.GrainId.GetGuidKey();
        PlayerData playerData = await _playerModelRepository.GetOrCreate(PlayerId);
        _dbid = playerData.Id;
    }

    public Task Echo(int seq)
    {
        return Task.CompletedTask;
    }

}


