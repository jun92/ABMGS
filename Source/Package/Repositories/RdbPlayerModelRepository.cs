using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SyncnetPlatform.Databases;
using System;
using System.Linq;

namespace SyncnetPlatform.Repositories;

public class RdbPlayerModelRepository : IPlayerModelRepository
{
    private readonly ILogger<RdbPlayerModelRepository> _logger;
    private readonly IDbContextFactory<SyncnetDbContext> _dbContextFactory;

    public RdbPlayerModelRepository(
        IDbContextFactory<SyncnetDbContext> dbContextFactory,
        ILogger<RdbPlayerModelRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }
    

    public async Task<PlayerData> GetOrCreate(Guid playerId, string playerName = "")
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        PlayerData? playerData = await dbContext.Players.Where(w => w.PlayerId == playerId).FirstOrDefaultAsync();
        if (playerData == null) 
        {
            playerData = new PlayerData
            {
                PlayerId = playerId,
                PlayerName = playerName
            };
            await dbContext.Players.AddAsync(playerData);
            await dbContext.SaveChangesAsync();
        }
        return playerData;
    }

    public async Task<PlayerData> Update(PlayerData playerData)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        dbContext.Players.Update(playerData);
        await dbContext.SaveChangesAsync();
        return playerData;
    }

 

    public async Task<PlayerData?> Get(int id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        PlayerData? playerData = await dbContext.Players.FindAsync(id);
        return playerData;
    }
}

public interface IPlayerModelRepository
{
    Task<PlayerData?> Get(int id);
    Task<PlayerData> GetOrCreate(Guid playerId, string playerName = "");
    Task<PlayerData> Update(PlayerData playerData);
}

public interface IExternalIdentityRepository
{
    Task<Guid> GetOrCreate(IdProviderType idProviderType, string idExternal);
    Task Remove(IdProviderType idProviderType, string idExternal);
}

public class RdbExternalIdentityRepository : IExternalIdentityRepository
{
    private readonly ILogger<RdbExternalIdentityRepository> _logger;
    private readonly IDbContextFactory<SyncnetDbContext> _dbContextFactory;

    //private readonly SyncnetDbContext _db;

    public RdbExternalIdentityRepository(
        ILogger<RdbExternalIdentityRepository> logger,
        IDbContextFactory<SyncnetDbContext> dbContextFactory

        )
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }
    public async Task<Guid> GetOrCreate(IdProviderType idProviderType, string idExternal)
    {
        var dbContext = _dbContextFactory.CreateDbContext();
        PlayerExternalIdentities? entity = await dbContext.ExternalIdentities
            .SingleOrDefaultAsync(p => 
            p.IdProvider == idProviderType &&
            p.IdExternal == idExternal);
        if (entity == null)
        {
            try
            {
                entity = new PlayerExternalIdentities
                {
                    Id = 0,
                    IdProvider = idProviderType,
                    IdExternal = idExternal,
                    SyncnetId = Guid.NewGuid(),
                    Created = DateTime.UtcNow
                };
                await dbContext.ExternalIdentities.AddAsync(entity);
                await dbContext.SaveChangesAsync();
            }
            catch(DbUpdateException ex) when (ex.InnerException is PostgresException pg)
            {
                if(pg.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    // Reload the entity.
                    entity = await dbContext.ExternalIdentities
                        .SingleAsync(p => 
                        p.IdProvider == idProviderType &&
                        p.IdExternal == idExternal);

                    return entity.SyncnetId;
                }
                else
                {
                    throw;
                }
            }
        }
        return entity.SyncnetId;
    }

    public async Task Remove(IdProviderType idProviderType, string idExternal)
    {
        var dbContext = _dbContextFactory.CreateDbContext();

        _ = await dbContext.ExternalIdentities
            .Where(w =>
                w.IdProvider == idProviderType &&
                w.IdExternal == idExternal)
            .ExecuteDeleteAsync();

    }
}