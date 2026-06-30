
namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerDataActor : IGrainWithGuidKey
{
    Task CreateNewPlayerData(string playerName);
}


