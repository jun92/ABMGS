using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;

namespace SyncnetPlatform.Network.Handlers;

public class PacketContextFactory : IPacketContextFactory
{
    private readonly IGrainFactory _grainFactory;

    public PacketContextFactory(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public PacketContext Create(Guid playerId, ILocalPlayer localPlayer)
    {
        return new PacketContext(playerId, _grainFactory, localPlayer);
    }
}

