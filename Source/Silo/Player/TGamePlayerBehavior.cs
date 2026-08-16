

using Google.FlatBuffers;
using SyncnetPlatform.Actors;
using TGame.Packets;
using Silo.Models;

namespace Silo.Player;

public class TGamePlayerCustomState : IPlayerCustomState
{
    
    public TGamePlayerCustomState()
    {
    }
    public byte[] Serialize(Dictionary<string, object?> playerState)
    {
        FlatBufferBuilder builder = new (4096);
        TGamePlayerCustomData.StartTGamePlayerCustomData(builder);
        TGamePlayerCustomData.AddWinCount(builder, (int)(playerState[TGamePlayerModelExtend.WinCount] ?? 0));
        TGamePlayerCustomData.AddLoseCount(builder, (int)(playerState[TGamePlayerModelExtend.LoseCount] ?? 0));
        TGamePlayerCustomData.AddPlayCount(builder, (int)(playerState[TGamePlayerModelExtend.PlayCount] ?? 0));
        builder.Finish(TGamePlayerCustomData.EndTGamePlayerCustomData(builder).Value);
        return builder.SizedByteArray();
    }

    public Dictionary<string, object?> Deserialize(byte[] data)
    {
        TGamePlayerCustomData customData = TGamePlayerCustomData.GetRootAsTGamePlayerCustomData(new ByteBuffer(data));
        return new Dictionary<string, object?>
        {
            {TGamePlayerModelExtend.WinCount, customData.WinCount},
            {TGamePlayerModelExtend.LoseCount, customData.LoseCount},
            {TGamePlayerModelExtend.PlayCount, customData.PlayCount},
        };
    }
}

public static class ActionCommand
{
    public const string GotWin = "Win";
    public const string GotLost = "Lost";
}

// TGame means Tic-Tac-Toe Game.
public class TGamePlayerBehavior : IPlayerCustomBehavior
{
    // public Dictionary<string, object?> DeserializePlayerExtendData(byte[] data)
    // {
    //     TGamePlayerCustomData customData = TGamePlayerCustomData.GetRootAsTGamePlayerCustomData(new ByteBuffer(data));
    //     return new Dictionary<string, object?>
    //     {
    //         {TGamePlayerModelExtend.WinCount, customData.WinCount},
    //         {TGamePlayerModelExtend.LoseCount, customData.LoseCount},
    //         {TGamePlayerModelExtend.PlayCount, customData.PlayCount},
    //     };
    // }
    private readonly IPlayerCustomState  _playerCustomState;

    public TGamePlayerBehavior(IPlayerCustomState playerCustomState)
    {
        _playerCustomState = playerCustomState;
    }

    public Task HandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public void OnJoinPlayRoom(PlayerState playerState, Guid playRoomId, bool isOwner, byte[]? roomState)
    {
        throw new NotImplementedException();
    }

    public Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> OnLogoutAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {
        throw new NotImplementedException();
    }

    // public byte[] SerializePlayerExtendData(Dictionary<string, object?> playerState, CancellationToken? cancellationToken = null)
    // {
    //     FlatBufferBuilder builder = new (4096);
    //     TGamePlayerCustomData.StartTGamePlayerCustomData(builder);
    //     TGamePlayerCustomData.AddWinCount(builder, (int)(playerState[TGamePlayerModelExtend.WinCount] ?? 0));
    //     TGamePlayerCustomData.AddLoseCount(builder, (int)(playerState[TGamePlayerModelExtend.LoseCount] ?? 0));
    //     TGamePlayerCustomData.AddPlayCount(builder, (int)(playerState[TGamePlayerModelExtend.PlayCount] ?? 0));
    //     builder.Finish(TGamePlayerCustomData.EndTGamePlayerCustomData(builder).Value);
    //     return builder.SizedByteArray();
    // }

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