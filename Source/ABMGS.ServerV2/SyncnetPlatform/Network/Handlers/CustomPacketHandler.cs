using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Actors.Player;
using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Dto;

namespace ABMGS.ServerV2.SyncnetPlatform.Network.Handlers;

public partial class CustomPacketHandler : PacketHandlerBase, ICustomPacketHandler
{
    protected Guid _playerId;
    public CustomPacketHandler(ILogger<PacketHandlerBase> logger, IClusterClient clusterClient) : base(logger, clusterClient)
    {
    }

    public void BindPlayer(Guid playerId)
    {
        _playerId = playerId;
    }

    [PacketHandler(typeof(LoginRequest))]
    public void HandleLoginRequest(LoginRequest loginRequest)
    {
        IPlayerActor player = GetPlayer(_playerId);
        _logger.LogInformation($"Id: {loginRequest.Id}, From: {loginRequest.From}, Count: {loginRequest.Count}");
    }
    [PacketHandler(typeof(MoveRequest))]
    public void HandleMoveRequest(MoveRequest moveRequest)
    {
        _logger.LogInformation($"Id: {moveRequest.Id}, X: {moveRequest.X}, Y: {moveRequest.Y}");
    }
   
}
