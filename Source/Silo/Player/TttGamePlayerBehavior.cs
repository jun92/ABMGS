

using Google.FlatBuffers;
using SyncnetPlatform.Actors;
using TGame.Packets;
using Silo.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Silo.Player;

public class TttGamePlayerExtendData : IPlayerExtendData
{
    private int _winCount = 0;
    private int _loseCount = 0;
    private int _playCount = 0;
    
    public byte[] Serialize(IReadOnlyDictionary<string, object?> playerState)
    {
        FlatBufferBuilder builder = new (4096);
        TGamePlayerCustomData.StartTGamePlayerCustomData(builder);
        TGamePlayerCustomData.AddWinCount(builder, (int)(playerState[TttGamePlayerModelExtend.WinCount] ?? 0));
        TGamePlayerCustomData.AddLoseCount(builder, (int)(playerState[TttGamePlayerModelExtend.LoseCount] ?? 0));
        TGamePlayerCustomData.AddPlayCount(builder, (int)(playerState[TttGamePlayerModelExtend.PlayCount] ?? 0));
        builder.Finish(TGamePlayerCustomData.EndTGamePlayerCustomData(builder).Value);
        return builder.SizedByteArray();
    }

    public void Initialize(IReadOnlyDictionary<string, object?> state)
    {
        FillInnerState(
            (int)(state[TttGamePlayerModelExtend.WinCount] ?? 0),
            (int)(state[TttGamePlayerModelExtend.LoseCount] ?? 0),
            (int)(state[TttGamePlayerModelExtend.PlayCount] ?? 0)
            );
    }
   
    public Dictionary<string, object?> Deserialize(byte[] data)
    {
        TGamePlayerCustomData customData = TGamePlayerCustomData.GetRootAsTGamePlayerCustomData(new ByteBuffer(data));
        FillInnerState(customData.WinCount, customData.LoseCount, customData.PlayCount);
        return new Dictionary<string, object?>
        {
            {TttGamePlayerModelExtend.WinCount, customData.WinCount},
            {TttGamePlayerModelExtend.LoseCount, customData.LoseCount},
            {TttGamePlayerModelExtend.PlayCount, customData.PlayCount},
        };
    }

    public Dictionary<string, object?> Deserialize()
    {
        return new Dictionary<string, object?>
        {
            {TttGamePlayerModelExtend.WinCount, _winCount},
            {TttGamePlayerModelExtend.LoseCount, _loseCount},
            {TttGamePlayerModelExtend.PlayCount, _playCount},
        };
    }

    private void FillInnerState(int winCount, int loseCount, int playCount)
    {
        _winCount = winCount;
        _loseCount = loseCount;
        _playCount = playCount;
    }
}

public static class ActionCommand
{
    public const string GotWin = "Win";
    public const string GotLost = "Lost";
}

// TGame means Tic-Tac-Toe Game.
public class TttGamePlayerBehavior(IPlayerExtendData playerExtendData) : IPlayerCustomBehavior
{
    public IPlayerExtendData GetPlayerCustomState() => playerExtendData; 

    public Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {
        // Storing them into your own.
        playerExtendData.Initialize(playerData.Extension);
        
        // Do something if you need pre-processing on your data. then return true for updating database.
        
        return Task.FromResult(false); // no need to update database. true if it needs.
    }

    public Task<bool> OnLogoutAsync(CancellationToken? cancellationToken = null)
    {
        
        return Task.FromResult(false); // return true if you need to update database.
    }
    
    public void OnJoinPlayRoom(PlayerState playerState, Guid playRoomId, bool isOwner, byte[]? roomState)
    {
    }
    
    public void UpdatePlayerExtendDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState)
    {
        // Func<byte[], PlayerState,  int> handler = actionType switch
        // {
        //     ActionCommand.GotWin => (data, state) =>
        //     {
        //         state[TttGamePlayerModelExtend.WinCount] = (int)state[TttGamePlayerModelExtend.WinCount] + 1;
        //         state[TttGamePlayerModelExtend.PlayCount] = (int)state[TttGamePlayerModelExtend.PlayCount] + 1;
        //     },
        //     ActionCommand.GotLost => (data, state) => { return 1;},
        // };
        // handler(actionParameters, playerState);
    }

}