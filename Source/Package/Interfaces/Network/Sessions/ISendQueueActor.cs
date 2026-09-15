using Orleans;
using System.Threading.Tasks;

namespace SyncnetPlatform.Interfaces.Network.Sessions;

public interface ISendQueueActor : IGrainWithGuidKey
{
    Task Register(ISendQueueObserver observer);
    Task Unregister();
    Task Push(byte[] data);
    Task IsValid();
}
