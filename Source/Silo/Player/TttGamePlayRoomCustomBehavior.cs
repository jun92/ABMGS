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
    Empty = 0,
    X = 1,
    O = 2
}

public struct Command
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

public interface ITttGamePlayRoomState : IPlayRoomCustomState
{
    int CurrentInCount { get; }
    Guid WinnerPlayerId { get; }
    Guid GetPlayerIdInTurn();
    void TurnToNextPlayer();
    bool PutMarket(int x, int y, Guid playerId);
    bool IsGameOver();
    List<Guid> GetBroadcastTargets();
    void AddPlayer(Guid id, Dictionary<string, object?> extendData);
    void RemovePlayer(Guid id);
    bool SetPlayerReady(Guid playerId, bool readyState);
}

public class TttGamePlayRoomState : ITttGamePlayRoomState
{
    private OrderedDictionary<Guid, bool> _playerReadyState = new();
    private int _turnIndex = 0;
    private Guid _currentTurnPlayerId = Guid.Empty;
    private readonly CellInfo[,] _playBoard = new CellInfo[3, 3];
    private const int MaxPlayerNum = 2;
    private Guid _winnerPlayerId = Guid.Empty;
    private readonly OrderedDictionary<Guid, Dictionary<string, object?>> _playerCustomStates = new();

    public Guid WinnerPlayerId
    {
        get => _winnerPlayerId;
    }

    public TttGamePlayRoomState()
    {
        ResetBoard();
    }

    public int CurrentInCount
    {
        get
        {
            return _playerCustomStates.Count; 
        }
    }

    private void ResetBoard()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                _playBoard[i, j] = new CellInfo();
            }
        }
    }

    public Guid GetPlayerIdInTurn()
    {
        var playerInTurn = _playerCustomStates.GetAt(_turnIndex);
        return playerInTurn.Key;
    }

    public void TurnToNextPlayer()
    {
        _turnIndex++;
        if (_turnIndex >= MaxPlayerNum )
        {
            _turnIndex = 0;
        }
    }

    public bool PutMarket(int x, int y, Guid playerId)
    {
        
        //early exit check.
        if (x < 0 || x > 2 || y < 0 || y > 2) return false;
        if (_playBoard[x, y].PlayerId != Guid.Empty) return false;
        
        // Decide player's mark symbol.
        CellState thisPlayerMark = CellState.X;
        if (_playerCustomStates.GetAt(0).Key == playerId)
        {
            thisPlayerMark = CellState.O;
        }
        _playBoard[x,y] = new CellInfo
        {
             PlayerId = playerId,
             MarkedTime = DateTime.UtcNow,
             State = thisPlayerMark
        };
        return true;
    }

    public bool IsGameOver()
    {
        // [=] vertical check.
        for (int i = 0; i < 3; i++)
        {
            if (_playBoard[0,i].PlayerId != Guid.Empty && 
                Enumerable.Range(0, 3).All(j => _playBoard[j, i].PlayerId.Equals(_playBoard[0, i].PlayerId)))
            {
                _winnerPlayerId = _playBoard[0, i].PlayerId;
                return true;
            }
        }

        // [||] horizonal check
        for (int i = 0; i < 3; i++)
        {
            if (_playBoard[i,0].PlayerId != Guid.Empty &&
                Enumerable.Range(0, 3).All(j => _playBoard[i, j].PlayerId.Equals(_playBoard[i, 0].PlayerId)))
            {
                _winnerPlayerId = _playBoard[i, 0].PlayerId;
                return true;
            }
        }

        if (_playBoard[1, 1].PlayerId != Guid.Empty)
        {
            //Diagonal check.
            // [ \ ] check
            if ( _playBoard[0, 0].PlayerId == _playBoard[1, 1].PlayerId &&
                 _playBoard[1, 1].PlayerId == _playBoard[2, 2].PlayerId)
            {
                _winnerPlayerId = _playBoard[1, 1].PlayerId;
                return true;
            }
            // [ / ] check
            if ( _playBoard[2, 0].PlayerId == _playBoard[1, 1].PlayerId &&
                 _playBoard[1, 1].PlayerId == _playBoard[0, 2].PlayerId)
            {
                _winnerPlayerId = _playBoard[1, 1].PlayerId;
                return true;
            }
        }

        if (_playBoard.Cast<CellInfo>().All(c => c.PlayerId != Guid.Empty))
        {
            _winnerPlayerId = Guid.Empty;
            return true;
        }
        return false;
    }

    public List<Guid> GetBroadcastTargets() => [.. _playerCustomStates.Select(c => c.Key)];
    

    public void AddPlayer(Guid id, Dictionary<string, object?> extendData)
    {
        _playerCustomStates.TryAdd(id, extendData);
        _playerReadyState.Add(id, false);
    }

    public void RemovePlayer(Guid id)
    {
        _playerCustomStates.Remove(id);
    }
    
    // For sharing the data with clients, In this case, I used FlatBuffer, but you can use any serializer you want. JSON, protoBuf.
    public byte[] Serialize()
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        
        List<Offset<ReadyState>>  readyStates = new List<Offset<ReadyState>>();
        foreach (var readyState in _playerReadyState)
        {
            StringOffset playerIdOffset = builder.CreateString(readyState.Key.ToString());
            ReadyState.StartReadyState(builder);
            ReadyState.AddIsReady(builder, readyState.Value);
            ReadyState.AddPlayerId(builder, playerIdOffset);
            Offset<ReadyState> offsetReadyState = ReadyState.EndReadyState(builder);
            readyStates.Add(offsetReadyState);
        }

        StringOffset currentTurnPlayerIdOffset = builder.CreateString(_currentTurnPlayerId.ToString());
        List<Offset<TGameCellInfo>> boradStates = [];
        foreach (var b in _playBoard)
        {
            StringOffset markedPlayerIdOffset = builder.CreateString(b.PlayerId.ToString());
            StringOffset markedTimeOffset = builder.CreateString(b.MarkedTime.ToString());
            TGameCellInfo.StartTGameCellInfo(builder);
            TGameCellInfo.AddMarkedPlayerId(builder, markedPlayerIdOffset);
            TGameCellInfo.AddMarkedTime(builder, markedTimeOffset);
            TGameCellInfo.AddMark(builder, Int32.Parse(b.State.ToString()) );
            Offset<TGameCellInfo> cellInfoOffset = TGameCellInfo.EndTGameCellInfo(builder);
            boradStates.Add(cellInfoOffset);
        }
        TGamePlayRoomState.StartTGamePlayRoomState(builder);
        TGamePlayRoomState.CreateReadyStateVector(builder, [.. readyStates]);
        TGamePlayRoomState.CreateBoardStateVector(builder, [.. boradStates]);
        TGamePlayRoomState.AddCurrentTurnPlayerId(builder, currentTurnPlayerIdOffset);

        Offset<TGamePlayRoomState> offset = TGamePlayRoomState.EndTGamePlayRoomState(builder);
        builder.Finish(offset.Value);

        return builder.SizedByteArray();
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
    private ITttGamePlayRoomState? _tttGamePlayRoomState;
    
    private Dictionary<Guid, Queue<byte[]>> _sendQueue = new();

    public Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync()
    {
        _tttGamePlayRoomState = playRoomCustomState as ITttGamePlayRoomState;
        
        return Task.FromResult(playRoomCustomState);
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        return Task.CompletedTask;
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
    public Task<(Dictionary<Guid, byte[]>?, byte[]?)> OnPlayerActionToPlayRoom(Guid playerId, string actionType,
        byte[] actionParameter, IPlayRoomSendBuffer sendBuffer)
    {
        switch (actionType)
        {
            case Command.Ready:
                HandleReqPlayerReady(actionParameter, playerId, sendBuffer);
                // play room state has changed. not player state
                return Task.FromResult<(Dictionary<Guid, byte[]>?, byte[]?)>((null, _tttGamePlayRoomState!.Serialize()));
            case Command.PutMarker:
                HandleReqPutMarker(actionParameter, playerId, sendBuffer);
                break;
        }
        return Task.FromResult<(Dictionary<Guid, byte[]>?, byte[]?)>((null, null));
    }

    private void HandleReqPutMarker(byte[] parameter, Guid playerId, IPlayRoomSendBuffer sendBuffer)
    {
        TGameReqActionPutItem putItem = TGameReqActionPutItem.GetRootAsTGameReqActionPutItem(new ByteBuffer(parameter));
        if (_tttGamePlayRoomState!.PutMarket(putItem.X, putItem.Y, playerId))
        {
            if (_tttGamePlayRoomState.IsGameOver())
            {
                if (_tttGamePlayRoomState.WinnerPlayerId == Guid.Empty)
                {
                    // Draw
                }
                else
                {
                    // Winner is : _tttGamePlayRoomState.WinnerPlayerId
                }
            }
        }
    }

    private void HandleReqPlayerReady(byte[] parameter, Guid playerId, IPlayRoomSendBuffer sendBuffer)
    {
        // Packet parsing.
        TGameReqActionSetReady readyState =
            TGameReqActionSetReady.GetRootAsTGameReqActionSetReady(new ByteBuffer(parameter));
        
        // Update play room custom states
        int result = OnReqPlayerReady(new Guid(readyState.PlayerId), readyState.ReadyState);
        if (result == 0)
        {
            //let's assume 0 means all players are ready and good to start a new game.
            // Use your serializer 
            FlatBufferBuilder builder = new FlatBufferBuilder(128);
            var offset = TGameNotifyGameStarted.CreateTGameNotifyGameStarted(builder, builder.CreateString(_tttGamePlayRoomState!.GetPlayerIdInTurn().ToString()));
            builder.Finish(offset.Value);
            byte[] dataToSend = builder.SizedByteArray();
            // 
            List<Guid> players = _tttGamePlayRoomState.GetBroadcastTargets();
            sendBuffer.BroadcastFiltered(players, dataToSend);
        }
    }

    private int OnReqPlayerReady(Guid playerId, bool readyState)
    {
        bool isAllReady = _tttGamePlayRoomState!.SetPlayerReady(playerId, readyState);
        if (isAllReady)
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
