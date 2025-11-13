using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Handlers;

public class SystemPacketHandler : PacketHandlerBase, ISystemPacketHandler
{
    protected Guid _playerId;
    public SystemPacketHandler(ILogger<SystemPacketHandler> logger, IClusterClient clusterClient) : base(logger, clusterClient)
    {
    }

    public void BindPlayer(Guid playerId)
    {
        _playerId = playerId;
    }

    
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        IPlayerActor player = GetPlayer(_playerId);
        await player.Echo(request.Seq);
    }

}
