namespace SyncnetPlatform.Interfaces.Actors;

public interface IPacketHandlerActor : IGrainWithGuidKey
{
    public Task InvokeHandler(byte[] data);
    public Task PushRecievedData(byte[] Data);
}


