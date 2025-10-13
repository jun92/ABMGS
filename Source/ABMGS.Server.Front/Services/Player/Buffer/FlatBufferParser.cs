using ABMGS.Server.Front.Interfaces.Networks;

namespace ABMGS.Server.Front.Services.Player.Buffer;

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
