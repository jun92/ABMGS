using Microsoft.Extensions.Logging;
using Orleans;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Sessions;

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
    protected async Task SendData(Guid toPlayerId, byte[] data)
    {
        ISendDataGrain sendDataGrain = _clusterClient.GetGrain<ISendDataGrain>(toPlayerId);
        await sendDataGrain.Send(data);

    }
}
