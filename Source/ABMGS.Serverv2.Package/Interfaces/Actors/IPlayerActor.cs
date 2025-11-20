using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerActor : IGrainWithGuidKey
{
    public Task Echo(int seq);
}


