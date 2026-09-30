using SyncnetPlatform.Network.Buffers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncnetPlatform.Actors;

public record PlayerActionResult(Dictionary<Guid, byte[]> PlayerUpdatedStats);

public record PlayRoomActionResult(byte[] PlayRoomUpdatedStat);

public interface IPlayRoomCustomEventHandler
{
    Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync();
    
    Task OnPlayRoomDestroyingAsync();

    Task<int> AddPlayerToPlayRoom(Guid id, byte[] playerExtendData);

    Task<(PlayerActionResult?, PlayRoomActionResult?)> ReqPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter, IPlayRoomSendBuffer sendBuffer);
    Task OnTimer(float delta);
}
