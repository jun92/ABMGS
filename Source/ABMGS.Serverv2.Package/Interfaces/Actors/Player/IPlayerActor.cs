namespace SyncnetPlatform.Interfaces.Actors.Player;

public interface IPlayerActor : IGrainWithGuidKey
{
    public Task Echo(int seq);
}


