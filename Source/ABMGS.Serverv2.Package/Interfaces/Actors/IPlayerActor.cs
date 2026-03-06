using SyncnetPlatform.Controllers;
using SyncnetPlatform.Network.Utils;
using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerActor : IGrainWithGuidKey
{
    public Task Echo(int seq);
    Task<string> GetPlayerName();
    Task SetIdProvider(SupportedPlatformType idpFrom);
    Task UpdatePlayerName(string newName);
}


