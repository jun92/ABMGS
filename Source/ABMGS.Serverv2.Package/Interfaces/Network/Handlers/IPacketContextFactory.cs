using SyncnetPlatform.Network.Handlers;

namespace SyncnetPlatform.Interfaces.Network.Handlers;

public interface IPacketContextFactory
{
    public PacketContext Create(Guid playerId);
}

