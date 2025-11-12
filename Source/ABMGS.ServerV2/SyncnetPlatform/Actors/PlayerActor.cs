using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Actors.Player;
using Microsoft.AspNetCore.Routing.Template;
using SyncnetPlatform.Dto;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace ABMGS.ServerV2.SyncnetPlatform.Actors;

/// <summary>
/// 세션에 연결되어 있는 플레이어의 모든 엔티티를 가지고 있는 상위 그레인,
/// Circular deadlock를 막기 위해 모든 하단 그레인들에 대한 호출 그래프를 관리하다.
/// </summary>
public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;
    }


}

public interface IPlayerData : IGrainWithGuidKey
{

}

public class PlayerDataActor: Grain, IPlayerData
{
    private readonly ILogger<PlayerDataActor> _logger;
    public PlayerDataActor(ILogger<PlayerDataActor> logger)
    {
        _logger = logger;
    }
}

public interface IPlayerInventory : IGrainWithGuidKey
{
    public void AddItem(Guid id);
    public void DeleteItem(Guid id);
}

public class PlayerInventoryActor : Grain, IPlayerInventory
{
    private readonly ILogger<PlayerInventoryActor> _logger;
    public PlayerInventoryActor(ILogger<PlayerInventoryActor> logger)
    {
        _logger = logger;
    }

    public void AddItem(Guid id)
    {
        throw new NotImplementedException();
    }

    public void DeleteItem(Guid id)
    {
        throw new NotImplementedException();
    }
}


