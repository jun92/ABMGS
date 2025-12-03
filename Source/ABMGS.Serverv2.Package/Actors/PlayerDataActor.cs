using Microsoft.Extensions.Logging;
using SyncnetPlatform.Databases;
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

public interface IPlayerDataExtendDataLoader
{
    Task<T> Load<T>(Guid playerId);
    Task Update<T>(T data);
}

public interface IPlayerDataExtendDataActor : IGrainWithGuidKey
{

}
public class PlayerDataExtendDataActor : Grain, IPlayerDataExtendDataActor
{
    private readonly ILogger<PlayerDataExtendDataActor> _logger;
    private readonly SyncnetDbContext _syncnetDbContext;
    private readonly IPlayerDataExtendDataLoader _playerDataExtendDataLoader;
    public PlayerDataExtendDataActor(
        ILogger<PlayerDataExtendDataActor> logger,
        SyncnetDbContext syncnetDbContext,
        IPlayerDataExtendDataLoader playerDataExtendDataLoader)
    {
        _logger = logger;
        _syncnetDbContext = syncnetDbContext;
        _playerDataExtendDataLoader = playerDataExtendDataLoader;
    }

    public Task<T> LoadExtendData<T>(Guid playerid)
    {
        throw new NotImplementedException();
    }
}

