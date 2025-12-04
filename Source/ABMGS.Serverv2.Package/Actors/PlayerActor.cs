using Microsoft.Extensions.Logging;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Repositories;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace SyncnetPlatform.Actors;


public interface IPlayerBehavior
{

}

public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;
    private readonly IPlayerModelRepositoy _repository;

    public PlayerActor(
        ILogger<PlayerActor> logger,
        IPlayerModelRepositoy repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        //Guid PlayerId = this.GetPrimaryKey();
        //PlayerDataModel? playerDataModel = await _repository.Get(PlayerId);
        //if (playerDataModel == null) {
        //    await _repository.Create(new PlayerDataModel
        //    {
        //        PlayerId = PlayerId,
        //        PlayerName = String.Empty,
        //        Introduction = String.Empty
        //    });
        //}
        
    }
    
    

    public async Task Echo(int seq)
    {
        
    }

}


