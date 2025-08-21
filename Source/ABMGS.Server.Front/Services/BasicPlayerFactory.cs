namespace ABMGS.Server.Front.Services;

public class BasicPlayerFactory : IPlayerFactory
{
    public IPlayer CreatePlayer(Guid id)
    {
        return new Player(id);
    }
}
