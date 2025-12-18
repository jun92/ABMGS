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
    private readonly SyncnetDbContext _syncnetDbContext;

    public RdbPlayerModelRepository(
        SyncnetDbContext syncnetDbContext,
        ILogger<RdbPlayerModelRepository> logger)
    {
        _syncnetDbContext = syncnetDbContext;
        _logger = logger;
    }


    public async Task Create(PlayerData newPlayerModel)
    {
        await _syncnetDbContext.Players.AddAsync(newPlayerModel);
    }

    public async Task<PlayerData?> Get(Guid playerId)
    {
        var playerModel = await _syncnetDbContext.Players.Where(w => w.PlayerId == playerId).FirstOrDefaultAsync();
        return playerModel;
    }

    public async Task<PlayerData?> Get(int id)
    {
        var playerModel = await _syncnetDbContext.Players.Where(w => w.Id == id).FirstOrDefaultAsync();
        return playerModel;
    }
}

public interface IPlayerModelRepository
{
    Task Create(PlayerData newPlayerDataModel);
    Task<PlayerData?> Get(Guid playerId);
    Task<PlayerData?> Get(int id);
}

public interface IExternalIdentityRepository
{
    Task<Guid> GetOrCreate(IdProviderType idProviderType, string idExternal);
    Task Remove(IdProviderType idProviderType, string idExternal);
}

public class RdbExternalIdentityRepository : IExternalIdentityRepository
{
    private readonly ILogger<RdbExternalIdentityRepository> _logger;
    private readonly SyncnetDbContext _db;

    public RdbExternalIdentityRepository(
        ILogger<RdbExternalIdentityRepository> logger,
        SyncnetDbContext db

        )
    {
        _logger = logger;
        _db = db;
    }
    public async Task<Guid> GetOrCreate(IdProviderType idProviderType, string idExternal)
    {
        PlayerExternalIdentities? entity = await _db.ExternalIdentities
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
                await _db.ExternalIdentities.AddAsync(entity);
                await _db.SaveChangesAsync();
            }
            catch(DbUpdateException ex) when (ex.InnerException is PostgresException pg)
            {
                if(pg.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    // Reload the entity.
                    entity = await _db.ExternalIdentities
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
        _ = await _db.ExternalIdentities
            .Where(w =>
                w.IdProvider == idProviderType &&
                w.IdExternal == idExternal)
            .ExecuteDeleteAsync();

    }
}