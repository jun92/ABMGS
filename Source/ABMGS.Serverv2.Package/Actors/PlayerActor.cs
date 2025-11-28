using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Repositories;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace SyncnetPlatform.Actors;


public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;
    private readonly IPlayerModelRepositoy _repository;

    public PlayerActor(ILogger<PlayerActor> logger,
        IPlayerModelRepositoy repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        
    }
    
    

    public async Task Echo(int seq)
    {
        
    }

}


