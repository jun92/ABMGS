using Google.FlatBuffers;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Databases;

public class MyPlayerBehavior : IPlayerCustomBehavior
{
    public Task HandleCustomPacket(byte[] customPacket)
    {
        return Task.CompletedTask;
    }

    public void OnJoinPlayRoom<TPlayRoomMetaData>(PlayerState playerState, Guid playRoomId, bool isOwner, TPlayRoomMetaData? roomMetaData = default) where TPlayRoomMetaData : IPlayRoomCustomState
    {
    }

    public Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null)
    {
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
        PlayerCustomData.AddCustomLevel(builder, playerState[PlayerDataColumn.CustomLevel] as int? ?? 1);
        var offset = PlayerCustomData.EndPlayerCustomData(builder);
        builder.Finish(offset.Value);
        return Task.FromResult(builder.SizedByteArray());
    }

    public void UpdatePlayerExtendDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState)
    {
        Action<byte[], PlayerState> handler = actionType.ToLower() switch
        {
            "gainexp" => (actionParameters, playerState) => 
            {
                int gainExp = BitConverter.ToInt32(actionParameters, 0);
                var currentCustomExp = playerState[PlayerDataColumn.CustomExp] as long? ?? 0;
                playerState[PlayerDataColumn.CustomExp] =  currentCustomExp + gainExp;
            },
            _ => (actionParameters, playerState) => { }
        };

        handler(actionParameters, playerState);
    }

    public byte[] SerializePlayerExtendData(Dictionary<string, object?> playerState, CancellationToken? cancellationToken = null)
    {
        return Array.Empty<byte>();
    }

    public Dictionary<string, object?> DeserializePlayerExtendData(byte[] data)
    {
        throw new NotImplementedException();
    }
};



