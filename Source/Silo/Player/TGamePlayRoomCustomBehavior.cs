using SyncnetPlatform.Actors;

namespace Silo.Player;


internal enum CellState
{
    Empty,
    X,
    O
}

internal class CellInfo
{
    public Guid PlayerId { get; set; } = Guid.Empty;
    public DateTime MarkedTime { get; set; } = DateTime.MinValue;
    public CellState State { get; set; } = CellState.Empty;
}

public class TGamePlayRoomState : IPlayRoomCustomState
{
    private List<Guid> _playerIds = new();
    private int _turnIndex = 0;
    private CellInfo[,] _playBoard = new CellInfo[3, 3]; 
    
    
    // For sharing the data with clients
    public byte[] Serialize()
    {
        throw new NotImplementedException();
    }

    public void Deserialize(byte[] serialized)
    {
        throw new NotImplementedException();
    }
}

public class TGamePlayRoomCustomBehavior : IPlayRoomCustomEventHandler
{
    public Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync()
    {
        throw new NotImplementedException();
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        throw new NotImplementedException();
    }

    public Task OnHandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public IPlayRoomCustomState DeserializePlayRoomState(byte[] roomMetaData)
    {
        throw new NotImplementedException();
    }

    public byte[] SerializePlayRoomState(IPlayRoomCustomState playRoomCustomState)
    {
        throw new NotImplementedException();
    }

    public Task AddPlayerToPlayRoom(Guid id, byte[] playerMetadata)
    {
        throw new NotImplementedException();
    }

    public Task<(Dictionary<Guid, byte[]>, byte[]?)> OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter)
    {
        throw new NotImplementedException();
    }

    public Task OnTimer(float delta)
    {
        throw new NotImplementedException();
    }
}
