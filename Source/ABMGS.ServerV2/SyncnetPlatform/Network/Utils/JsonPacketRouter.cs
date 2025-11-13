using SyncnetPlatform.Interfaces.Network.Utils;

namespace SyncnetPlatform.Network.Utils;

public class JsonPacketRouter : IPacketRouter
{
    private readonly ILogger<JsonPacketRouter> _logger;
    public JsonPacketRouter(ILogger<JsonPacketRouter> logger)
    {
        _logger = logger;
    }
    public void Execute(object packet)
    {
        throw new NotImplementedException();
    }
}
