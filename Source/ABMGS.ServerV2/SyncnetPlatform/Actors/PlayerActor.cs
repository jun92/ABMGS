using SyncnetPlatform.Interfaces.Actors.Player;
using Google.FlatBuffers;
using System.Net.WebSockets;
using SyncnetPlatform.Protocols.Generated;

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
            PacketFactory factory = new PacketFactory();
            await _webSocket.SendAsync(new ArraySegment<byte>(factory.BuildPong(seq)), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        else
        {
            _logger.LogError($"{nameof(Echo)} was called without valid WebSocket handle");
        }
    }
}

public class PacketFactory
{
    private readonly ILogger<PacketFactory>? _logger;
    public PacketFactory(ILogger<PacketFactory> logger)
    {
        _logger = logger; 
    }
    public PacketFactory()
    {
        _logger = null;
    }

    public byte[] BuildPong(int seq)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        Offset<Pong> pongOffset = Pong.CreatePong(builder, ++seq);
        builder.Finish(pongOffset.Value);
        return builder.SizedByteArray();
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


