namespace ABMGS.Server.Front.Services;

public class FlatBufferParser : IParser
{
    public PacketType Parse<PacketType>(byte[] data) where PacketType : IPacket
    {
        // Implement FlatBuffer parsing logic here
        throw new NotImplementedException();
    }
}
