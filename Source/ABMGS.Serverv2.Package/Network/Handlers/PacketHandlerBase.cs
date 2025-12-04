using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Services;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Sessions;

namespace SyncnetPlatform.Network.Handlers;

public class PacketContext
{
    private readonly IGrainFactory _grainFactory;
    private readonly Guid _playerId;

    public PacketContext(Guid playerId, IGrainFactory grainFactory)
    {
        _playerId = playerId;
        _grainFactory = grainFactory;
    }
    public Guid GetPlayerId() => _playerId;
    public IPlayerActor GetPlayer() => _grainFactory.GetGrain<IPlayerActor>(_playerId);
    public IPlayerDataActor GetPlayerData() => _grainFactory.GetGrain<IPlayerDataActor>(_playerId);
    public IPlayerInventoryActor GetPlayerInventory() => _grainFactory.GetGrain<IPlayerInventoryActor>(_playerId);
    public async Task SendData(Guid toPlayerId, byte[] data)
    {
        ISendDataGrain sendDataGrain = _grainFactory.GetGrain<ISendDataGrain>(_playerId);
        await sendDataGrain.Send(data);
    }
}
