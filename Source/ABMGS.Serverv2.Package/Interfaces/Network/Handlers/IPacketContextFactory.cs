using SyncnetPlatform.Network.Handlers;

namespace SyncnetPlatform.Actors;

public interface IPacketContextFactory
{
    public PacketContext Create(Guid playerId);
}

