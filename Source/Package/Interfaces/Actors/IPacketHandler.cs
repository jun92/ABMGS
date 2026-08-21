using Orleans;
using SyncnetPlatform.Network.Utils;
using System.Threading.Tasks;

namespace SyncnetPlatform.Interfaces.Actors;

public interface IPacketHandlerActor : IGrainWithGuidKey
{
    ValueTask InvokeHandler(byte[] data);
    ValueTask PushRecievedData(byte[] Data);
}


