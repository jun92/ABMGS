namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerInventoryActor : IGrainWithGuidKey
{
    public void AddItem(Guid id);
    public void DeleteItem(Guid id);
}


