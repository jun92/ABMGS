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
    private readonly ILogger<PlayerActor> _logger;
    private readonly IDbContextFactory<SyncnetDbContext> _dbFactory;


    public PlayerActor(
        ILogger<PlayerActor> logger,
        IDbContextFactory<SyncnetDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbFactory = dbContextFactory;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Guid PlayerId = GrainContext.GrainId.GetGuidKey();

        await using var dbContext = await _dbFactory.CreateDbContextAsync();


        //PlayerData? playerData = await _repository.Get(PlayerId);
        //if (playerData == null)
        //{
        //}


        //Load basic information from database, if nothing, create new player.
    }

    public Task Echo(int seq)
    {
        return Task.CompletedTask;
    }

}


