using SyncnetPlatform.Actors;
using SyncnetPlatform.Databases;

public class MyPlayerBehavior : IPlayerCustomBehavior
{
    public Task HandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public Task<bool> OnLoginAsync(PlayerData playerData, CancellationToken? cancellationToken = null)
    {
        Console.WriteLine(playerData[PlayerDataColumn.Title]);

        playerData[PlayerDataColumn.Title] = "I am king";


        return Task.FromResult<bool>(true);
    }

    public Task<bool> OnLogoutAsync(PlayerData playerData, CancellationToken? cancellationToken = null)
    {

        return Task.FromResult<bool>(false);
    }
};




