using ABMGS.Server.Front.Interfaces.Players;

namespace ABMGS.Server.Front.Services.Player;

public class DefaultPlayerFactory : IPlayerFactory
{
    public IPlayer CreatePlayer(Guid id)
    {
        return new Player(id);
    }
}
