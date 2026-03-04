using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SyncnetPlatform.Controllers;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Repositories;

namespace SyncnetPlatform.Actors;

public interface IPlayerBehavior
{

}

public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;
    private readonly IPlayerModelRepository _playerModelRepository;
    
    // player data
    private int _dbid;
    private string _name = String.Empty;
    protected SupportedPlatformType _idpFrom;

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepository playerModelRepository
        )
    {
        _logger = logger;
        _playerModelRepository = playerModelRepository;
        
    }

    public async Task SetIdProvider(SupportedPlatformType idpFrom)
    {
        _idpFrom = idpFrom;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Guid PlayerId = GrainContext.GrainId.GetGuidKey();
        PlayerData playerData = await _playerModelRepository.GetOrCreate(PlayerId);
        _dbid = playerData.Id;
        await base.OnActivateAsync(cancellationToken);
    }

    public Task Echo(int seq)
    {
        return Task.CompletedTask;
    }

}


