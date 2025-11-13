using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Dto;

namespace ABMGS.ServerV2.SyncnetPlatform.Network.Handlers;

public class SystemPacketHandler : ISystemPacketHandler
{
    private readonly ILogger<SystemPacketHandler> _logger;
    protected Guid _playerId;

    public SystemPacketHandler(ILogger<SystemPacketHandler> logger)
    {
        _logger = logger;
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

}
