using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Network.Utils;
using System.Net.WebSockets;

namespace SyncnetPlatform.Actors;

/// <summary>
/// 세션에 연결되어 있는 플레이어의 모든 엔티티를 가지고 있는 상위 그레인,
/// Circular deadlock를 막기 위해 모든 하단 그레인들에 대한 호출 그래프를 관리하다.
/// </summary>
public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;
    private WebSocket? _webSocket;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;
        _webSocket = null;
    }

    public WebSocket WebSocketHandle { private get => _webSocket; set => _webSocket = value; }

    public async Task Echo(int seq)
    {
        if (_webSocket != null)
        {
            await _webSocket.SendAsync(
                new ArraySegment<byte>(SyncnetPacketBuilder.Build<PongArgs>(new PongArgs(seq + 1))), 
                WebSocketMessageType.Binary, 
                true, 
                CancellationToken.None);
        }
        else
        {
            _logger.LogError($"{nameof(Echo)} was called without valid WebSocket handle");
        }
    }
}

public interface IPlayerDataActor : IGrainWithGuidKey
{

}

public class PlayerDataActor: Grain, IPlayerDataActor
{
    private readonly ILogger<PlayerDataActor> _logger;
    public PlayerDataActor(ILogger<PlayerDataActor> logger)
    {
        _logger = logger;
    }
}

public interface IPlayerInventoryActor : IGrainWithGuidKey
{
    public void AddItem(Guid id);
    public void DeleteItem(Guid id);
}

public class PlayerInventoryActor : Grain, IPlayerInventoryActor
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


