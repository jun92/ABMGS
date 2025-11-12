using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Actors.Player;

namespace ABMGS.ServerV2.SyncnetPlatform.Network.Handlers;

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
}
