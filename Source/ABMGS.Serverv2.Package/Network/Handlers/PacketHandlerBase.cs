using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Services;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Sessions;

namespace SyncnetPlatform.Network.Handlers;


public class GameObjectFactoryService : IGrainService
{
    private readonly IGrainFactory _grainFactory;
    public GameObjectFactoryService(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }
    public IPlayerActor GetPlayer(Guid playerGuid) => _grainFactory.GetGrain<IPlayerActor>(playerGuid);
    public IPlayerDataActor GetPlayerData(Guid playerGuid) => _grainFactory.GetGrain<IPlayerDataActor>(playerGuid);
    public IPlayerInventoryActor GetPlayerInventory(Guid playerGuid) => _grainFactory.GetGrain<IPlayerInventoryActor>(playerGuid);
    public async Task SendData(Guid toPlayerId, byte[] data)
    {
        ISendDataGrain sendDataGrain = _grainFactory.GetGrain<ISendDataGrain>(toPlayerId);
        await sendDataGrain.Send(data);
    }
}
public class PacketContext
{
    public Guid PlayerId {get; set;}
     
}

public class PacketHandlerBase
{
    protected readonly ILogger<PacketHandlerBase> _logger;

    protected readonly IGrainFactory _grainFactory;

    public PacketHandlerBase(
        ILogger<PacketHandlerBase> logger,
        IGrainFactory grainFactory
        )
    {
        _logger = logger;
        _grainFactory = grainFactory;
    }

    protected IPlayerActor GetPlayer(Guid playerGuid) => _grainFactory.GetGrain<IPlayerActor>(playerGuid);
    protected IPlayerDataActor GetPlayerData(Guid playerGuid) => _grainFactory.GetGrain<IPlayerDataActor>(playerGuid);
    protected IPlayerInventoryActor GetPlayerInventory(Guid playerGuid) => _grainFactory.GetGrain<IPlayerInventoryActor>(playerGuid);
    protected async Task SendData(Guid toPlayerId, byte[] data)
    {
        ISendDataGrain sendDataGrain = _grainFactory.GetGrain<ISendDataGrain>(toPlayerId);
        await sendDataGrain.Send(data);

    }
}
