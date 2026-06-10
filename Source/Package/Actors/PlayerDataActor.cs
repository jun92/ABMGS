using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Actors;

namespace SyncnetPlatform.Actors;

public interface IPlayerDataBehavior
{
    Task OnCreateNewPlayer(PlayerDataContext ctx);
}
public class PlayerDataContext
{
    public Guid PlayerId { get; set; }
    public SyncnetDbContext? Db { get; set; }
    public ILogger? logger { get; set; }

}

public class PlayerDataActor: Grain, IPlayerDataActor
{
    private readonly ILogger<PlayerDataActor> _logger;
    private readonly IEnumerable<IPlayerDataBehavior> _playerDataBehavior;
    private readonly IDbContextFactory<SyncnetDbContext> _dbFactory; 
    public PlayerDataActor(
        ILogger<PlayerDataActor> logger, 
        IEnumerable<IPlayerDataBehavior> playerDataBehaviors,
        IDbContextFactory<SyncnetDbContext> dbFactory
        )
    {
        _logger = logger;
        _playerDataBehavior = playerDataBehaviors;
        _dbFactory = dbFactory;
    }

    protected PlayerDataContext CreatePlayerDataContext() => new()
    {
        PlayerId = this.GetPrimaryKey(),
        Db = _dbFactory.CreateDbContext(),
        logger = _logger
    };

    public async Task CreateNewPlayerData(string playerName)
    {
        var _db = await _dbFactory.CreateDbContextAsync();
        await _db.Players.AddAsync(new PlayerData
        {
            PlayerId = this.GetPrimaryKey(),
            PlayerName = playerName
        });

        foreach (var behavior in _playerDataBehavior)
        {
            await behavior.OnCreateNewPlayer(CreatePlayerDataContext());
        }

        await _db.SaveChangesAsync();
    }
}

//public interface IPlayerDataExtendDataLoader
//{
//    Task<T> Load<T>(Guid playerId);
//    Task Update<T>(T data);
//}

//public interface IPlayerDataExtendDataActor : IGrainWithGuidKey
//{

//}
//public class PlayerDataExtendDataActor : Grain, IPlayerDataExtendDataActor
//{
//    private readonly ILogger<PlayerDataExtendDataActor> _logger;
//    private readonly SyncnetDbContext _syncnetDbContext;
//    private readonly IPlayerDataExtendDataLoader _playerDataExtendDataLoader;
//    public PlayerDataExtendDataActor(
//        ILogger<PlayerDataExtendDataActor> logger,
//        SyncnetDbContext syncnetDbContext,
//        IPlayerDataExtendDataLoader playerDataExtendDataLoader)
//    {
//        _logger = logger;
//        _syncnetDbContext = syncnetDbContext;
//        _playerDataExtendDataLoader = playerDataExtendDataLoader;
//    }

//    public Task<T> LoadExtendData<T>(Guid playerid)
//    {
//        throw new NotImplementedException();
//    }
//}
