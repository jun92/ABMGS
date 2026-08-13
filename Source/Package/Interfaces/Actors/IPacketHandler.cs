using SyncnetPlatform.Network.Utils;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPacketHandlerActor : IGrainWithGuidKey
{
    ValueTask InvokeHandler(byte[] data);
    ValueTask PushRecievedData(byte[] Data);
}


