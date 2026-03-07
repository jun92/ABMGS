namespace SyncnetPlatform.Network.Utils;

public class FlatBufferPacketBuildException : Exception
{
    public FlatBufferPacketBuildException(string message, Exception ex) :base(message, ex)
    {
    }
}
