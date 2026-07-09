using Google.FlatBuffers;
using SyncnetPlatform.Actors;

namespace Silo.Player;

public class MyPlayRoomMetaData : IPlayRoomMetaData
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

public class MyPlayRoomCustomBehavior : IPlayRoomCustomEventHandler<MyPlayRoomMetaData>
{
    public MyPlayRoomMetaData DeserializePlayRoomMetaData(byte[] roomMetaData)
    {
        throw new NotImplementedException();
    }

    public IPlayRoomMetaData? InitializePlayRoomMetaData()
    {
        return new MyPlayRoomMetaData
        {
            TestField01 = 15,
            TestField02 = "MyPlayRoomMetaData2",
            TestField03 = true
        };
    }

    public Task OnHandleCustomPacket(byte[] customPacket)
    {
        throw new NotImplementedException();
    }

    public Task OnPlayRoomDestroyingAsync()
    {
        throw new NotImplementedException();
    }

    public Task<MyPlayRoomMetaData> OnPlayRoomInitializingAsync()
    {
        throw new NotImplementedException();
    }

    public byte[] SerializePlayRoomMetaData(IPlayRoomMetaData playRoomMetaData)
    {
        throw new NotImplementedException();
    }
}
