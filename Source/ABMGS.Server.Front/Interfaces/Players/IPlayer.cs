using ABMGS.Server.Front.Interfaces.Networks;

namespace ABMGS.Server.Front.Interfaces.Players;

public interface IPlayer
{
    public Guid Id();
    public INetworkBuffer GetBuffer();
}
