namespace SyncnetPlatform.Exceptions;

public class FlatBufferPacketBuildException : Exception
{
    public FlatBufferPacketBuildException(string message, Exception ex) :base(message, ex)
    {
    }
}
