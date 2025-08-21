namespace ABMGS.Server.Front.Interfaces.Networks;

public interface IParser
{
    public PacketType Parse<PacketType>(byte[] data) where PacketType : IPacket;
    public byte[] Pack<PacketType>(PacketType packet) where PacketType : IPacket;
}
