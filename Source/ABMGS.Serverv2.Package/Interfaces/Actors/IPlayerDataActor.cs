namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerDataActor : IGrainWithGuidKey
{
    Task<T> LoadExtendData<T>(Guid playerId);
    Task UpdateExtendData<T>(Guid playerId, T extendData);
}


