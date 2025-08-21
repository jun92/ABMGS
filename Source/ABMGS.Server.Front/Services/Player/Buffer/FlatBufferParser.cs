namespace ABMGS.Server.Front.Services;

public class FlatBufferParser : IParser
{
    public byte[] Pack<PacketType>(PacketType packet) where PacketType : IPacket
    {
        throw new NotImplementedException();
    }

    public PacketType Parse<PacketType>(byte[] data) where PacketType : IPacket
    {
        // Implement FlatBuffer parsing logic here
        throw new NotImplementedException();
    }
}
