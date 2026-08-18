using SyncnetPlatform.Actors;

namespace Silo.Player;


public enum CellState
{
    Empty,
    X,
    O
}

public class CellInfo
{
    public Guid PlayerId { get; set; } = Guid.Empty;
    public DateTime MarkedTime { get; set; } = DateTime.MinValue;
    public CellState State { get; set; } = CellState.Empty;
}

public class TGamePlayRoomState : IPlayRoomCustomState
{
    // Storing player id
    private List<Guid> _playerIds = new();
    // Stating player ready or not 
    private List<bool> _playerReadyState = new();
    // Indicate who is currently on.
    private int _turnIndex = 0;
    // Board state. 
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
    IPlayRoomCustomState _playRoomCustomState;
    private List<Guid> _playerIds = new();
    private Dictionary<Guid, IPlayerCustomState> _playerCustomStates = new();

    public TGamePlayRoomCustomBehavior(IPlayRoomCustomState playRoomCustomState)
    {
        _playRoomCustomState = playRoomCustomState;
    }
    
    public Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync()
    {
        _playerIds.Clear();
        return Task.FromResult(_playRoomCustomState);
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        _playerIds.Clear();
        return Task.CompletedTask;
    }

    public IPlayRoomCustomState DeserializePlayRoomState(byte[] roomMetaData)
    {
        return _playRoomCustomState;
    }

    public byte[] SerializePlayRoomState(IPlayRoomCustomState playRoomCustomState)
    {
        throw new NotImplementedException();
    }

    public Task AddPlayerToPlayRoom(Guid id, byte[] playerCustomState)
    {
        _playerIds.Add(id);
        
        //_playerCustomStates.Add(id, _playRoomCustomState.Deserialize(playerCustomState));

        return Task.CompletedTask;
    }

    public Task<(Dictionary<Guid, byte[]>, byte[]?)> OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter)
    {
        throw new NotImplementedException();
        // return (new Dictionary<Guid, byte[]>(), new byte[8]);
    }

    public Task OnTimer(float delta)
    {
        throw new NotImplementedException();
    }
}
