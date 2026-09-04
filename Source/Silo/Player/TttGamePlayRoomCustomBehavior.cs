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

public struct Command
{
    public const string Ready = "Ready";
    public const string PutMarker = "Put";
}

public class TttGamePlayRoomCustomBehavior(
    IPlayRoomCustomState playRoomCustomState,
    TttGamePacketSerializer  tttGamePacketSerializer
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
                return Task.FromResult<(Dictionary<Guid, byte[]>?, byte[]?)>((null, _tttGamePlayRoomState!.Serialize()));
        }
        return Task.FromResult<(Dictionary<Guid, byte[]>?, byte[]?)>((null, null));
    }

    private void HandleReqPutMarker(byte[] parameter, Guid playerId, IPlayRoomSendBuffer sendBuffer)
    {
        TGameReqActionPutItem putItem = TGameReqActionPutItem.GetRootAsTGameReqActionPutItem(new ByteBuffer(parameter));
        
        if (!_tttGamePlayRoomState!.PutMarket(putItem.X, putItem.Y, playerId)) return;
        if (!_tttGamePlayRoomState!.IsGameOver()) return;

        if (_tttGamePlayRoomState.WinnerPlayerId == Guid.Empty)
        {
            // Draw
        }
        else
        {
            // Winner is : _tttGamePlayRoomState.WinnerPlayerId
        }

        byte[] gameEndedPacket = tttGamePacketSerializer.SerializeNotiftGameEnded(_tttGamePlayRoomState.WinnerPlayerId);
        sendBuffer.BroadcastToAll(gameEndedPacket);
    }

    private void HandleReqPlayerReady(byte[] parameter, Guid playerId, IPlayRoomSendBuffer sendBuffer)
    {
        // Packet parsing.
        TGameReqActionSetReady readyState = tttGamePacketSerializer.DeserializeGameReqActionSetReady(parameter);
        
        // Update play room custom states
        int result = OnReqPlayerReady(new Guid(readyState.PlayerId), readyState.ReadyState);
        if (result == 0)
        {
            //let's assume 0 means all players are ready and good to start a new game.
            // Use your serializer 
            byte[] dataToSend = tttGamePacketSerializer.SerializeNotiftGameStarted(_tttGamePlayRoomState!.GetPlayerIdInTurn());

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
            return 0;
        }
        return -1;
    }


    public Task OnTimer(float delta)
    {
        throw new NotImplementedException();
    }
}