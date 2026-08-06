namespace SyncnetPlatform.Actors;

public interface IPlayRoomCustomState
{
    byte[] Serialize();
    void Deserialize(byte[] serialized);
}
