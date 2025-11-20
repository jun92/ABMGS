namespace SyncnetPlatform.Interfaces.Actors;

public interface IPacketHandler : IGrainWithGuidKey
{
    public Task InvokeHandler(byte[] data);
    public Task PushRecievedData(byte[] Data);
}


