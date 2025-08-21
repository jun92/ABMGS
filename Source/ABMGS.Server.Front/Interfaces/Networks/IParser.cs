namespace ABMGS.Server.Front.Services;

public interface IParser
{
    public PacketType Parse<PacketType>(byte[] data) where PacketType : IPacket;
    public byte[] Pack<PacketType>(PacketType packet) where PacketType : IPacket;
}
