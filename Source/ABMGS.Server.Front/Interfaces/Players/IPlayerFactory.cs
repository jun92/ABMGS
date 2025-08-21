namespace ABMGS.Server.Front.Interfaces.Players;

public interface IPlayerFactory
{
    public IPlayer CreatePlayer(Guid id);
}
