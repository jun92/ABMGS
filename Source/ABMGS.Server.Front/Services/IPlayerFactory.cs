namespace ABMGS.Server.Front.Services;

public interface IPlayerFactory
{
    public IPlayer CreatePlayer(Guid id);
}
