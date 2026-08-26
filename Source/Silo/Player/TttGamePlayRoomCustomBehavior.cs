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
        TGamePlayRoomState.CreateReadyStateVector(builder, readyStates.ToArray());
        TGamePlayRoomState.CreateBoardStateVector(builder, boradStates.ToArray());
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
                TGameReqActionSetReady readyState = TGameReqActionSetReady.GetRootAsTGameReqActionSetReady(new ByteBuffer(actionParameter));
                int result = OnReqPlayerReady(new Guid(readyState.PlayerId), readyState.ReadyState);
                if (result == 0)
                {
                    //let's assume 0 means all players are ready and good to start a new game.
                    
                    // sendBuffer.PushBuffer()
                }

                // play room state has changed. not player state
                return Task.FromResult<(Dictionary<Guid, byte[]>?, byte[]?)>((null, _tttGamePlayRoomState!.Serialize()));
            case Command.PutMarker:
                break;
        }
        return Task.FromResult<(Dictionary<Guid, byte[]>?, byte[]?)>((null, null));
    }

    private int OnReqPlayerReady(Guid playerId, bool readyState)
    {
        bool IsAllReady = _tttGamePlayRoomState!.SetPlayerReady(playerId, readyState);
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
