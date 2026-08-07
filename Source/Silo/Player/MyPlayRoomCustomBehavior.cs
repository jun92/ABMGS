using Google.FlatBuffers;
using SyncnetPlatform.Actors;

namespace Silo.Player;

public class MyPlayRoomCustomState : IPlayRoomCustomState
{
    public int TestField01 { get; set; }
    public string TestField02 { get; set; } = String.Empty;
    public bool TestField03 { get; set; }
    public void Deserialize(byte[] serialized)
    {
        var playRoomCreationMetaData = PlayRoomCreationMetaData.GetRootAsPlayRoomCreationMetaData(new ByteBuffer(serialized));
        TestField01 = playRoomCreationMetaData.ExtField1;
        TestField02 = playRoomCreationMetaData.ExtField2;
        TestField03 = playRoomCreationMetaData.ExtField3;
    }

    public byte[] Serialize()
    {
        FlatBufferBuilder builder = new(4096);
        StringOffset f2 = builder.CreateString(TestField02);
        PlayRoomCreationMetaData.StartPlayRoomCreationMetaData(builder);
        PlayRoomCreationMetaData.AddExtField1(builder, TestField01);
        PlayRoomCreationMetaData.AddExtField2(builder, f2);
        PlayRoomCreationMetaData.AddExtField3(builder, TestField03);
        builder.Finish(PlayRoomCreationMetaData.EndPlayRoomCreationMetaData(builder).Value);
        return builder.SizedByteArray();
    }
}

public class MyGameRoomLogic
{
    public void Init()
    {

    }
    public void Deinit()
    {

    }
}

public class MyPlayRoomCustomBehavior : IPlayRoomCustomEventHandler
{
    private readonly IPlayRoomCustomState _playRoomCustomState;

    public MyPlayRoomCustomBehavior(IPlayRoomCustomState playRoomCustomState)
    {
        _playRoomCustomState = playRoomCustomState;
    }

    public Task AddPlayerToPlayRoom(Guid id, byte[] playerMetadata)
    {
        throw new NotImplementedException();
    }

    public IPlayRoomCustomState DeserializePlayRoomMetaData(byte[] roomMetaData)
    {
        return _playRoomCustomState;
    }

    public IPlayRoomCustomState? InitializePlayRoomMetaData()
    {
         
        return _playRoomCustomState; 
    }

    public Task OnHandleCustomPacket(byte[] customPacket)
    {
        return Task.CompletedTask;
    }

    public Task OnPlayerAction(string actionName, byte[] actionParameter)
    {
        return Task.CompletedTask;
    }

    //public Task OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter)
    //{
    //    throw new NotImplementedException();
    //}

    public Task OnPlayRoomDestroyingAsync()
    {
        return Task.CompletedTask;
    }

    public Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync()
    {
        return Task.FromResult(_playRoomCustomState);
    }

    public Task OnTimer(float delta)
    {
        throw new NotImplementedException();
    }

    public Task<(Dictionary<Guid, byte[]>, byte[])> OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter)
    {
        throw new NotImplementedException();
    }
}
