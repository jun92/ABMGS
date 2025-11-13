using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Actors.Player;
using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Network.Attributes;
using ABMGS.ServerV2.SyncnetPlatform.Protocos.FlatBuffer.Generated;
using System.Threading.Tasks;

namespace ABMGS.ServerV2.SyncnetPlatform.Network.Handlers;

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

    [PacketHandler(typeof(LoginRequest))]
    public void HandleLoginRequest(LoginRequest request)
    {

    }
    [PacketHandler(typeof(MoveRequest))]
    public void HandleMoveRequest(MoveRequest request)
    {

    }
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        IPlayerActor player = GetPlayer(_playerId);
        await player.Echo(request.Seq);
    }

}
