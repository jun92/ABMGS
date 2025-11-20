using SyncnetPlatform.Interfaces.Actors;
using Microsoft.Extensions.Logging;

namespace SyncnetPlatform.Network.Handlers;

public class PacketHandlerBase
{
    protected readonly ILogger<PacketHandlerBase> _logger;

    protected readonly IClusterClient _clusterClient;

    public PacketHandlerBase(
        ILogger<PacketHandlerBase> logger,
        IClusterClient clusterClient
        )
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    protected IPlayerActor GetPlayer(Guid playerGuid) => _clusterClient.GetGrain<IPlayerActor>(playerGuid);
    protected IPlayerDataActor GetPlayerData(Guid playerGuid) => _clusterClient.GetGrain<IPlayerDataActor>(playerGuid);
    protected IPlayerInventoryActor GetPlayerInventory(Guid playerGuid) => _clusterClient.GetGrain<IPlayerInventoryActor>(playerGuid);
}
