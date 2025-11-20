using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Handlers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace SyncnetPlatform.Actors;


public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;

    }

    public async override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        
    }

    

    public async Task Echo(int seq)
    {
        
    }

}


