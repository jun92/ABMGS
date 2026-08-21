using Google.FlatBuffers;
using Silo.Models;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Network.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TGame.Packets;

namespace Silo.Player;


public enum CellState
{
    Empty,
    X,
    O
}

public class Command
{
    public const string Ready = "Ready";
    public const string PutMarker = "Put";
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
    public Dictionary<Guid, bool> _playerReadyState = new();
    // Indicate who is currently on.
    public int _turnIndex = 0;
    public Guid _currentTurnPlayerId = Guid.Empty;
    // Board state. 
    public CellInfo[,] _playBoard = new CellInfo[3, 3];

    public int MaxPlayerNum = 2;

    public int CurrentInCount
    {
        get
        {
            return _playerCustomStates.Count(); 
        }
    }
    
    private readonly Dictionary<Guid, Dictionary<string, object?>> _playerCustomStates = new();

    public void AddPlayer(Guid id, Dictionary<string, object?> extendData)
    {
        _playerCustomStates.TryAdd(id, extendData);
        _playerReadyState.Add(id, false);
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

    public bool SetPlayerReady(Guid playerId, bool  readyState)
    {
        _playerReadyState[playerId] = readyState;
        return _playerReadyState.Count(c => c.Value == true) == MaxPlayerNum;
    }
}

public class TttGamePlayRoomCustomBehavior(
    IPlayRoomCustomState playRoomCustomState
    ) : IPlayRoomCustomEventHandler
{
    private TttGamePlayRoomState? _tttGamePlayRoomState;
    
    private Dictionary<Guid, Queue<byte[]>> _sendQueue = new();

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

    public Task<int> AddPlayerToPlayRoom(Guid id, byte[] playerExtendDataArray)
    {
        if (playerExtendDataArray.Length == 0)
        {
            _tttGamePlayRoomState!.AddPlayer(id, new Dictionary<string, object?>(capacity:0));
            return Task.FromResult(0);
        }

        if (_tttGamePlayRoomState!.CurrentInCount >= 2)
        {
            return Task.FromResult(-1);
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
        return Task.FromResult(0);
    }
    public Task<(Dictionary<Guid, byte[]>, byte[]?)> OnPlayerActionToPlayRoom(Guid playerId, string actionType,
        byte[] actionParameter, IPlayRoomSendBuffer sendBuffer)
    {
        switch (actionType)
        {
            case Command.Ready:
                TGameReqActionSetReady readyState = TGameReqActionSetReady.GetRootAsTGameReqActionSetReady(new ByteBuffer(actionParameter));
                int result = OnReqPlayerReady(new Guid(readyState.PlayerId), readyState.ReadyState);
                ;
                break;
            case Command.PutMarker:
                break;
        }
        return Task.FromResult<(Dictionary<Guid, byte[]>, byte[]?)>(([], []));
    }

    private int OnReqPlayerReady(Guid playerId, bool readyState)
    {
        bool IsAllReady = _tttGamePlayRoomState.SetPlayerReady(playerId, readyState);
        if (IsAllReady)
        {
            // Start a new game.
        }
        return 0;
    }

    public Task OnTimer(float delta)
    {
        throw new NotImplementedException();
    }
}
