using ABMGS.Server.Front.Interfaces.Players;

namespace ABMGS.Server.Front.Services.Player;

public class DefaultPlayerFactory 
{
    public PlayerType CreatePlayer<PlayerType>(Guid id) where PlayerType : IPlayer
    {
        return (PlayerType)Activator.CreateInstance(typeof(PlayerType), id);
    }
}
