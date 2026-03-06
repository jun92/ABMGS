using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SyncnetPlatform.Controllers;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Network.Utils;
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
    protected int _dbid;
    protected string _name = String.Empty;
    protected SupportedPlatformType _idpFrom;

    protected PlayerData _playerData = new();

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
        _playerData = await _playerModelRepository.GetOrCreate(PlayerId);
        _dbid = _playerData.Id;
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if(_playerData != null)
        {
            await _playerModelRepository.Update(_playerData);
        }
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task Echo(int seq)
    {
        return Task.CompletedTask;
    }

    public async Task UpdatePlayerName(string newName)
    {
        _playerData.PlayerName = newName;
    }

    public async Task<string> GetPlayerName()
    {
        return _playerData.PlayerName; 
    }

}


