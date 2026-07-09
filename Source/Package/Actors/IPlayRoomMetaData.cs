namespace SyncnetPlatform.Actors;

public interface IPlayRoomMetaData
{
    byte[] Serialize();
    void Deserialize(byte[] serialized);
}
