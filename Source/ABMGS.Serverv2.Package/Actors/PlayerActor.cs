using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace SyncnetPlatform.Actors;



public interface IPacketHandler : IGrainWithGuidKey
{
    Task InvokeHandler(byte[] data);
    Task PushRecievedData(byte[] Data);
}

public class PacketHandlingActor : IPacketHandler
{
    private readonly FlatBufferPacketRouter _routeTable;
    private readonly ILogger<PacketHandlingActor> _logger;
    private readonly ConcurrentQueue<byte[]> _receiveQueue;
    public PacketHandlingActor(ILogger<PacketHandlingActor> logger, FlatBufferPacketRouter routeTable)
    {
        _logger = logger;
        _receiveQueue = new ConcurrentQueue<byte[]>();

        //PacketWrapper packet = PacketWrapper.GetRootAsPacketWrapper(BuildDummyPacket());
        _routeTable = routeTable;
        _routeTable.BuildParamExtractionFuncs<PacketWrapper>();
        _routeTable.BuildPacketHandlerFunctions<PacketHandlingActor>(this);
    }

    public async Task InvokeHandler(byte[] data)
    {
        _routeTable.Execute(PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data)));
    }

    public async Task PushRecievedData(byte[] Data)
    {
        _receiveQueue.Enqueue(Data);
    }

    [PacketHandler(typeof(Dummy))]
    public async Task HandleDummpy(Dummy dummpy)
    {
        _logger.LogError("Dummy packet received. Are you dummy?");
    }


    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        _logger.LogInformation($"HandlePing, Seq is {request.Seq}");
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request)
    {
        _logger.LogError("This should not be called.");
    }
    //protected ByteBuffer BuildDummyPacket()
    //{
    //    FlatBufferBuilder flatBufferBuilder = new FlatBufferBuilder(1024);
    //    Offset<Dummy> dummy = Dummy.CreateDummy(flatBufferBuilder, 0);
    //    Offset<PacketWrapper> wrapper = PacketWrapper.CreatePacketWrapper(flatBufferBuilder, SystemPacket.Dummy, dummy.Value);
    //    flatBufferBuilder.Finish(wrapper.Value);
    //    return flatBufferBuilder.DataBuffer;
    //}
}


/// <summary>
/// 세션에 연결되어 있는 플레이어의 모든 엔티티를 가지고 있는 상위 그레인,
/// Circular deadlock를 막기 위해 모든 하단 그레인들에 대한 호출 그래프를 관리하다.
/// </summary>
public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    [GameSpecificProperty]
    private string name;
    [GameSpecificProperty]
    private string displayName;
    [GameSpecificProperty]
    private int _level;
    [GameSpecificProperty]
    private double _exp;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;

    }

    public async override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        
    }

    

    public async Task Echo(int seq)
    {
        
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


