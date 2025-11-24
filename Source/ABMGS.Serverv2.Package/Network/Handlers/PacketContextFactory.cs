using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Handlers;

namespace SyncnetPlatform.Actors;

public class PacketContextFactory : IPacketContextFactory
{
    private readonly IGrainFactory _grainFactory;

    public PacketContextFactory(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public PacketContext Create(Guid playerId)
    {
        return new PacketContext(playerId, _grainFactory);
    }
}

