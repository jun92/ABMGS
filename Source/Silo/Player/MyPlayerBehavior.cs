using Google.FlatBuffers;
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
        //Console.WriteLine(playerData[PlayerDataColumn.Title]);

        //playerData[PlayerDataColumn.Title] = "I am king";


        return Task.FromResult<bool>(false);
    }

    public Task<bool> OnLogoutAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {

        return Task.FromResult<bool>(false);
    }

    public Task<byte[]> OverrideCustomDataSerialize(Dictionary<string, object?> playerState, CancellationToken? cancellationToken = null)
    {
        var builder = new FlatBufferBuilder(4096);

        PlayerCustomData.StartPlayerCustomData(builder);
        PlayerCustomData.AddCustomExp(builder, playerState[PlayerDataColumn.CustomExp] as long? ?? 0);
        PlayerCustomData.AddCustomLevel(builder, playerState[PlayerDataColumn.CustomLevel] as int? ?? 0);
        var offset = PlayerCustomData.EndPlayerCustomData(builder);
        builder.Finish(offset.Value);
        return Task.FromResult(builder.SizedByteArray());
    }

    public void UpdatePlayerCustomDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState)
    {
        Action<byte[], PlayerState> handler = actionType.ToLower() switch
        {
            "gainexp" => (actionParameters, playerState) => 
            {
                int gainExp = BitConverter.ToInt32(actionParameters, 0);
                playerState[PlayerDataColumn.CustomExp] = playerState[PlayerDataColumn.CustomExp] as long? ?? 0 + gainExp;
            },
            _ => (actionParameters, playerState) => { }
        };

        handler(actionParameters, playerState);
    }
};




