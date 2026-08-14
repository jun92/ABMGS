

using Google.FlatBuffers;
using SyncnetPlatform.Actors;
using TGame.Packets;
using Silo.Models;

namespace Silo.Player;

// TGame means Tic-Tac-Toe Game.
public class TGamePlayerBehavior : IPlayerCustomBehavior
{
    public Dictionary<string, object?> DeserializePlayerExtendData(byte[] data)
    {
        FlatBufferBuilder builder = new (4096);
        
        
        throw new NotImplementedException();
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

    public byte[] SerializePlayerExtendData(Dictionary<string, object?> playerState, CancellationToken? cancellationToken = null)
    {
        FlatBufferBuilder builder = new (4096);
        TGamePlayerCustomData.StartTGamePlayerCustomData(builder);
        TGamePlayerCustomData.AddWinCount(builder, (int)(playerState[TGamePlayerModelExtend.WinCount] ?? 0));
        TGamePlayerCustomData.AddLoseCount(builder, (int)(playerState[TGamePlayerModelExtend.LoseCount] ?? 0));
        TGamePlayerCustomData.AddPlayCount(builder, (int)(playerState[TGamePlayerModelExtend.PlayCount] ?? 0));
        builder.Finish(TGamePlayerCustomData.EndTGamePlayerCustomData(builder).Value);
        return builder.SizedByteArray();
        
    }

    public void UpdatePlayerExtendDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState)
    {
        throw new NotImplementedException();
    }

}