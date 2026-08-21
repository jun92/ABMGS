using SyncnetPlatform.Network.Buffers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncnetPlatform.Actors;

public interface IPlayRoomCustomEventHandler
{
    Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync();
    
    Task OnPlayRoomDestroyingAsync();
    
    IPlayRoomCustomState DeserializePlayRoomState(byte[] roomMetaData);
    byte[]  SerializePlayRoomState(IPlayRoomCustomState playRoomCustomState);

    Task<int> AddPlayerToPlayRoom(Guid id, byte[] playerExtendData);

    Task<(Dictionary<Guid, byte[]>, byte[]?)> OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter, IPlayRoomSendBuffer sendBuffer);
    Task OnTimer(float delta);
}
