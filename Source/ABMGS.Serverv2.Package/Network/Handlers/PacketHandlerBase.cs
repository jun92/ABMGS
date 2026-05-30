using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Services;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Utils;

namespace SyncnetPlatform.Network.Handlers;

public class PacketContext
{
    private readonly IGrainFactory _grainFactory;
    private readonly Guid _playerId;
    private readonly ILocalPlayer _localPlayer;

    public PacketContext(Guid playerId, IGrainFactory grainFactory, ILocalPlayer localPlayer)
    {
        _playerId = playerId;
        _grainFactory = grainFactory;
        _localPlayer = localPlayer;
    }
    public Guid GetPlayerId() => _playerId;
    public ILocalPlayer GetPlayer() => _localPlayer;
    public IPlayerDataActor GetPlayerData() => _grainFactory.GetGrain<IPlayerDataActor>(_playerId);
    public IPlayerInventoryActor GetPlayerInventory() => _grainFactory.GetGrain<IPlayerInventoryActor>(_playerId);
    //public async Task SendData(Guid toPlayerId, byte[] data)
    //{
    //    ISendDataGrain sendDataGrain = _grainFactory.GetGrain<ISendDataGrain>(_playerId);
    //    await sendDataGrain.Send(data);
    //}
    public async Task SendData<SyncnetPacketType>(SyncnetPacketType data) where SyncnetPacketType : IPacketBuildArgs
    {
        ISendDataGrain sendDataGrain = _grainFactory.GetGrain<ISendDataGrain>(_playerId);
        await sendDataGrain.Send(SyncnetPacketBuilder.Build<SyncnetPacketType>(data));
    }

    public async Task SendDataRaw(byte[] data)
    {
        ISendDataGrain sendDataGrain = _grainFactory.GetGrain<ISendDataGrain>(_playerId);
        await sendDataGrain.Send(data);
    }
}
