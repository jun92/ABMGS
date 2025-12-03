using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Databases;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Repositories;

public class rdbPlayerModelRepository : IPlayerModelRepositoy
{
    private readonly ILogger<rdbPlayerModelRepository> _logger;
    private readonly SyncnetDbContext _syncnetDbContext;

    public rdbPlayerModelRepository(
        SyncnetDbContext syncnetDbContext,
        ILogger<rdbPlayerModelRepository> logger)
    {
        _syncnetDbContext = syncnetDbContext;
        _logger = logger;
    }


    public async Task Create(PlayerDataModel newPlayerModel)
    {
        await _syncnetDbContext.players.AddAsync(newPlayerModel);
    }

    public async Task<PlayerDataModel?> Get(Guid playerId)
    {
        var playerModel = await _syncnetDbContext.players.Where(w => w.PlayerId == playerId).FirstOrDefaultAsync();
        return playerModel;
    }

    public async Task<PlayerDataModel?> Get(int id)
    {
        var playerModel = await _syncnetDbContext.players.Where(w => w.Id == id).FirstOrDefaultAsync();
        return playerModel;
    }
}

public interface IPlayerModelRepositoy
{
    Task Create(PlayerDataModel newPlayerDataModel);
    Task<PlayerDataModel?> Get(Guid playerId);
    Task<PlayerDataModel?> Get(int id);
}