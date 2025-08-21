using ABMGS.Server.Front.Interfaces.Networks;

namespace ABMGS.Server.Front.Services;

public interface IPlayer
{
    public Guid Id();
    public INetworkBuffer GetBuffer();
}
