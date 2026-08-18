

using Google.FlatBuffers;
using SyncnetPlatform.Actors;
using TGame.Packets;
using Silo.Models;

namespace Silo.Player;

public class TGamePlayerCustomState : IPlayerCustomState
{
    private int _winCount = 0;
    private int _loseCount = 0;
    private int _playCount = 0;
    
    public byte[] Serialize(IReadOnlyDictionary<string, object?> playerState)
    {
        FlatBufferBuilder builder = new (4096);
        TGamePlayerCustomData.StartTGamePlayerCustomData(builder);
        TGamePlayerCustomData.AddWinCount(builder, (int)(playerState[TGamePlayerModelExtend.WinCount] ?? 0));
        TGamePlayerCustomData.AddLoseCount(builder, (int)(playerState[TGamePlayerModelExtend.LoseCount] ?? 0));
        TGamePlayerCustomData.AddPlayCount(builder, (int)(playerState[TGamePlayerModelExtend.PlayCount] ?? 0));
        builder.Finish(TGamePlayerCustomData.EndTGamePlayerCustomData(builder).Value);
        return builder.SizedByteArray();
    }

    public void Initialize(IReadOnlyDictionary<string, object?> state)
    {
        FillInnerState(
            (int)(state[TGamePlayerModelExtend.WinCount] ?? 0),
            (int)(state[TGamePlayerModelExtend.LoseCount] ?? 0),
            (int)(state[TGamePlayerModelExtend.PlayCount] ?? 0)
            );
    }
   
    public Dictionary<string, object?> Deserialize(byte[] data)
    {
        TGamePlayerCustomData customData = TGamePlayerCustomData.GetRootAsTGamePlayerCustomData(new ByteBuffer(data));
        FillInnerState(customData.WinCount, customData.LoseCount, customData.PlayCount);
        return new Dictionary<string, object?>
        {
            {TGamePlayerModelExtend.WinCount, customData.WinCount},
            {TGamePlayerModelExtend.LoseCount, customData.LoseCount},
            {TGamePlayerModelExtend.PlayCount, customData.PlayCount},
        };
    }

    public Dictionary<string, object?> Deserialize()
    {
        return new Dictionary<string, object?>
        {
            {TGamePlayerModelExtend.WinCount, _winCount},
            {TGamePlayerModelExtend.LoseCount, _loseCount},
            {TGamePlayerModelExtend.PlayCount, _playCount},
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
public class TGamePlayerBehavior(IPlayerCustomState playerCustomState) : IPlayerCustomBehavior
{
    public IPlayerCustomState GetPlayerCustomState() => playerCustomState; 

    public Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {
        // Storing them into your own.
        playerCustomState.Initialize(playerData.Extension);
        
        // Do something if you need pre-processing on your data. then return true for updating database.
        
        return Task.FromResult(false); // no need to update database. true if it needs.
    }

    public Task<bool> OnLogoutAsync(CancellationToken? cancellationToken = null)
    {
        return Task.FromResult(false);
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
        //         state[TGamePlayerModelExtend.WinCount] = (int)state[TGamePlayerModelExtend.WinCount] + 1;
        //         state[TGamePlayerModelExtend.PlayCount] = (int)state[TGamePlayerModelExtend.PlayCount] + 1;
        //     },
        //     ActionCommand.GotLost => (data, state) => { return 1;},
        // };
        // handler(actionParameters, playerState);
    }

}