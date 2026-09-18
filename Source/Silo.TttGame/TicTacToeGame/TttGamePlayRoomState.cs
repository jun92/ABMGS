using Google.FlatBuffers;
using Silo.Models;
using TGame.Packets;

namespace Silo.Player;

public class TttGamePlayRoomState : ITttGamePlayRoomState
{
    private OrderedDictionary<Guid, bool> _playerReadyState = new();
    private int _turnIndex = 0;
    private Guid _currentTurnPlayerId = Guid.Empty;
    private readonly CellInfo[,] _playBoard = new CellInfo[3, 3];
    private const int MaxPlayerNum = 2;
    private Guid _winnerPlayerId = Guid.Empty;
    private readonly OrderedDictionary<Guid, Dictionary<string, object?>> _playerCustomStates = new();

    #region For Test verification
    public CellInfo[,] BoardState => _playBoard;
    public OrderedDictionary<Guid, bool> PlayerReadyState => _playerReadyState;
    public int CurrentInCount { get => _playerCustomStates.Count; }
    public Guid WinnerPlayerId { get => _winnerPlayerId; }
    #endregion

    public TttGamePlayRoomState()
    {
        ResetBoard();
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
             MarkedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
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

    private void IncreaseState(string customDataName, Dictionary<string, object?> extendData)
    {
        extendData?[customDataName] = (int)(extendData[customDataName] ?? 0) + 1;
    }

    private Dictionary<string, object?>? GetExtendData(Guid playerId)
    {
        if (!_playerCustomStates.TryGetValue(playerId, out Dictionary<string, object?>? extendData))
        {
            return null;
        }
        return extendData;
    }

    public void IncreaseWinCount(Guid playerId)
    {
        if (GetExtendData(playerId) is { } extendData)
        {
            IncreaseState(TttGamePlayerModelExtend.WinCount, extendData);
        }
    }

    public void IncreaseLoseCount(Guid playerId)
    {
        if (GetExtendData(playerId) is { } extendData)
        {
            IncreaseState(TttGamePlayerModelExtend.LoseCount, extendData);
        }
    }

    public void IncreasePlayCount(Guid playerId)
    {
        if (GetExtendData(playerId) is { } extendData)
        {
            IncreaseState(TttGamePlayerModelExtend.PlayCount, extendData);
        }
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
            StringOffset markedTimeOffset = builder.CreateString(b.MarkedTime);
            TGameCellInfo.StartTGameCellInfo(builder);
            TGameCellInfo.AddMarkedPlayerId(builder, markedPlayerIdOffset);
            TGameCellInfo.AddMarkedTime(builder, markedTimeOffset);
            TGameCellInfo.AddMark(builder, (int)Enum.Parse<CellState>(b.State.ToString()));
            Offset<TGameCellInfo> cellInfoOffset = TGameCellInfo.EndTGameCellInfo(builder);
            boradStates.Add(cellInfoOffset);
        }

        VectorOffset readyStateOffset = TGamePlayRoomState.CreateReadyStateVector(builder, [.. readyStates]);
        VectorOffset boardStateOffset = TGamePlayRoomState.CreateBoardStateVector(builder, [.. boradStates]);
        
        TGamePlayRoomState.StartTGamePlayRoomState(builder);
        TGamePlayRoomState.AddReadyState(builder, readyStateOffset);
        TGamePlayRoomState.AddBoardState(builder, boardStateOffset);
        TGamePlayRoomState.AddCurrentTurnPlayerId(builder, currentTurnPlayerIdOffset);

        Offset<TGamePlayRoomState> offset = TGamePlayRoomState.EndTGamePlayRoomState(builder);
        builder.Finish(offset.Value);

        return builder.SizedByteArray();
    }

    public void Deserialize(byte[] serialized)
    {
        TGamePlayRoomState tgamePlayRoomState = TGamePlayRoomState.GetRootAsTGamePlayRoomState(new ByteBuffer(serialized));

        _currentTurnPlayerId = new Guid(tgamePlayRoomState.CurrentTurnPlayerId);

        //_playerReadyState.Clear();
        
        for (int i = 0; i < tgamePlayRoomState.ReadyStateLength; i++)
        {
            ReadyState? rs = tgamePlayRoomState.ReadyState(i);
            if (rs != null)
            {
                Guid id = new Guid(rs.Value.PlayerId);
                bool isReady = rs.Value.IsReady;
                _playerReadyState[id] = isReady;
            }
        }

        List<(int, int)> index1DTo2D =
        [
            (0, 0), (0, 1), (0, 2),
            (1, 0), (1, 1), (1, 2),
            (2, 0), (2, 1), (2, 2)
        ];

        for (int i = 0; i < tgamePlayRoomState.BoardStateLength; i++)
        {
            TGameCellInfo? gameCellInfo = tgamePlayRoomState.BoardState(i);
            if (gameCellInfo != null)
            {
                (int posX, int posY) = index1DTo2D[i];
                _playBoard[posX,posY].PlayerId = new Guid(gameCellInfo.Value.MarkedPlayerId);
                _playBoard[posX,posY].MarkedTime = gameCellInfo.Value.MarkedTime;
                _playBoard[posX,posY].State = (CellState)gameCellInfo.Value.Mark;
            }
        }
    }

    public bool SetPlayerReady(Guid playerId, bool  readyState)
    {
        _playerReadyState[playerId] = readyState;
        return _playerReadyState.Count(c => c.Value == true) == MaxPlayerNum;
    }
}
