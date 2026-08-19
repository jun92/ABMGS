using Google.FlatBuffers;
using Silo.Models;
using SyncnetPlatform.Actors;
using TGame.Packets;

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

public class TttGamePlayRoomState : IPlayRoomCustomState
{
    // Storing player id
    //public List<Guid> _playerIds = new();
    // Stating player ready or not 
    public List<bool> _playerReadyState = new();
    // Indicate who is currently on.
    public int _turnIndex = 0;
    public Guid _currentTurnPlayerId = Guid.Empty;
    // Board state. 
    public CellInfo[,] _playBoard = new CellInfo[3, 3]; 
    
    private readonly Dictionary<Guid, Dictionary<string, object?>> _playerCustomStates = new();

    public void AddPlayer(Guid id, Dictionary<string, object?> extendData)
    {
        _playerCustomStates.TryAdd(id, extendData);
    }

    public void RemovePlayer(Guid id)
    {
        _playerCustomStates.Remove(id);
    }
    
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

public class TttGamePlayRoomCustomBehavior(
    IPlayRoomCustomState playRoomCustomState
    ) : IPlayRoomCustomEventHandler
{
    private TttGamePlayRoomState? _tttGamePlayRoomState;

    public Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync()
    {
        _tttGamePlayRoomState = playRoomCustomState as TttGamePlayRoomState;
        
        return Task.FromResult(playRoomCustomState);
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        return Task.CompletedTask;
    }

    public IPlayRoomCustomState DeserializePlayRoomState(byte[] roomMetaData)
    {
        return playRoomCustomState;
    }

    public byte[] SerializePlayRoomState(IPlayRoomCustomState playRoomCustomState)
    {
        throw new NotImplementedException();
    }

    public Task AddPlayerToPlayRoom(Guid id, byte[] playerExtendDataArray)
    {
        if (playerExtendDataArray.Length == 0)
        {
            _tttGamePlayRoomState!.AddPlayer(id, new Dictionary<string, object?>(capacity:0));
            return Task.CompletedTask;
        }
        
        // FlatBuffer parsing, use your favorite serialize library. ex) protoBuf, json, etc.
        TGamePlayerCustomData playerExtendData = 
            TGamePlayerCustomData.GetRootAsTGamePlayerCustomData(new ByteBuffer(playerExtendDataArray));

        _tttGamePlayRoomState!.AddPlayer(id,new Dictionary<string, object?>
        {
            {TttGamePlayerModelExtend.WinCount, playerExtendData.WinCount},
            {TttGamePlayerModelExtend.LoseCount, playerExtendData.LoseCount},
            {TttGamePlayerModelExtend.PlayCount, playerExtendData.PlayCount},
        } );
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
