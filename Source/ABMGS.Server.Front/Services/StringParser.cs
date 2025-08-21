using System.Text;
using System.Text.Json;

namespace ABMGS.Server.Front.Services;

public class StringParser : IParser
{
    public PacketType Parse<PacketType>(byte[] data) where PacketType : IPacket
    {
        string jsonString = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<PacketType>(jsonString);
    }
}
