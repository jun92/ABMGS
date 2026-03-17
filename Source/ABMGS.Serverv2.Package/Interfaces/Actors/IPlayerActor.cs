using SyncnetPlatform.Controllers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Net.WebSockets;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPlayerActor : IGrainWithGuidKey
{
    public Task Echo(int seq);
    Task<string> GetPlayerName();
    Task<bool> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType);
    Task<bool> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType);
    Task SetIdProvider(SupportedPlatformType idpFrom);
    Task SetOnline(bool isOnline);
    Task UpdatePlayerName(string newName);
}


