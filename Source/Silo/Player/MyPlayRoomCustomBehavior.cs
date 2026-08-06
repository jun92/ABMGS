using Google.FlatBuffers;
using SyncnetPlatform.Actors;

namespace Silo.Player;

public class MyPlayRoomMetaData : IPlayRoomCustomState
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
    private readonly MyGameRoomLogic _myGameRoomLogic;
    private readonly IPlayRoomCustomState _playRoomMetaData;

    public MyPlayRoomCustomBehavior(IPlayRoomCustomState myPlayRoomMetaData, MyGameRoomLogic gameRoomLogic)
    {
        _myGameRoomLogic = gameRoomLogic;
        _playRoomMetaData = myPlayRoomMetaData;
    }

    public Task AddPlayerToPlayRoom(Guid id, byte[] playerMetadata)
    {
        throw new NotImplementedException();
    }

    public IPlayRoomCustomState DeserializePlayRoomMetaData(byte[] roomMetaData)
    {
        _playRoomMetaData.Deserialize(roomMetaData);
        return _playRoomMetaData;
    }

    public IPlayRoomCustomState? InitializePlayRoomMetaData()
    {

        return _playRoomMetaData; 
    }

    public Task OnHandleCustomPacket(byte[] customPacket)
    {
        return Task.CompletedTask;
    }

    public Task OnPlayerAction(string actionName, byte[] actionParameter)
    {
        return Task.CompletedTask;
    }

    public Task OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter)
    {
        throw new NotImplementedException();
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        return Task.CompletedTask;
    }

    public Task<IPlayRoomCustomState> OnPlayRoomInitializingAsync()
    {
        _myGameRoomLogic.Init();
        return Task.FromResult(_playRoomMetaData);
    }

    public Task OnTimer(float delta)
    {
        throw new NotImplementedException();
    }

    public byte[] SerializePlayRoomMetaData(IPlayRoomCustomState playRoomMetaData)
    {
        return _playRoomMetaData.Serialize();
    }
}
