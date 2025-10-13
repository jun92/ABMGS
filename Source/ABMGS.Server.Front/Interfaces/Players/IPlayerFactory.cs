namespace ABMGS.Server.Front.Interfaces.Players;

public interface IPlayerFactory
{
    public PlayerType CreatePlayer<PlayerType>(Guid id) where PlayerType : IPlayer;
}
