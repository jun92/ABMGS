using SyncnetPlatform.Actors;
using SyncnetPlatform.Databases;

public class MyPlayerBehavior : IPlayerCustomBehavior
{
    public Task HandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {
        Console.WriteLine(playerData[PlayerDataColumn.Title]);

        playerData[PlayerDataColumn.Title] = "I am king";


        return Task.FromResult<bool>(true);
    }

    public Task<bool> OnLogoutAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {

        return Task.FromResult<bool>(false);
    }

    public Task<byte[]> OverrideCustomDataSerialize(Dictionary<string, object?> playerState, CancellationToken? cancellationToken = null)
    {
        return Task.FromResult(new byte[1]);
    }

    public void UpdatePlayerCustomDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState)
    {
        throw new NotImplementedException();
    }
};




