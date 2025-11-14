namespace SyncnetPlatform.Interfaces.Network.Utils;

public interface IPacketRouter
{
    public void Execute(object packet);
}
