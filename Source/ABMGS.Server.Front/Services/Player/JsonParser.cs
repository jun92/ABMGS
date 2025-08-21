using System.Text;
using System.Text.Json;

namespace ABMGS.Server.Front.Services;

public class JsonParser : IParser
{
    public byte[] Pack<PacketType>(PacketType packet) where PacketType : IPacket
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet));
    }

    public PacketType Parse<PacketType>(byte[] data) where PacketType : IPacket
    {
        return JsonSerializer.Deserialize<PacketType>(Encoding.UTF8.GetString(data));
    }
}
