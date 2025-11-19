using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;
using SyncnetPlatform.Interfaces.Actors.Player;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
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

public interface IPacketObserver : IGrainObserver
{
    Task NewPacketArrived();
}

public class PacketHandlingActor : Grain, IPacketHandler, IPacketObserver
{
    private readonly IPacketRouter _routeTable;
    private readonly ILogger<PacketHandlingActor> _logger;
    private readonly ConcurrentQueue<byte[]> _receiveQueue;
    private readonly ConcurrentQueue<byte[]> _sendQueue;
    private ObserverManager<IPacketObserver>? _packetObserverManager;
    public PacketHandlingActor(ILogger<PacketHandlingActor> logger, IPacketRouter routeTable)
    {
        _logger = logger;
        _routeTable = routeTable;
        _receiveQueue = new ConcurrentQueue<byte[]>();
        _sendQueue = new ConcurrentQueue<byte[]>();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _packetObserverManager = new ObserverManager<IPacketObserver>(TimeSpan.FromDays(1), _logger);
        _packetObserverManager.Subscribe(this, this);
        _routeTable.BuildParamExtractionFuncs<PacketWrapper>();
        _routeTable.BuildPacketHandlerFunctions<PacketHandlingActor>(this);
        

    }
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _packetObserverManager?.Unsubscribe(this);
        _packetObserverManager?.Clear();
    }


    public async Task InvokeHandler(byte[] data)
    {
        _routeTable.Execute(PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data)));
    }

    public async Task PushRecievedData(byte[] Data)
    {
        _receiveQueue.Enqueue(Data);
        await _packetObserverManager.Notify(s => s.NewPacketArrived());
    }
    public async Task NewPacketArrived()
    {
        if(_receiveQueue.TryDequeue(out byte[]? newDataArrived))
        {
            if(newDataArrived != null)
            {
                await InvokeHandler(newDataArrived);
            }
        }
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

        //new PongArgs(Seq+1)
        byte[] SendBackData = SyncnetPacketBuilder.Build(new PongArgs(request.Seq + 1));

        ISendDataGrain sendDataGrain = GrainFactory.GetGrain<ISendDataGrain>(this.GetGrainId().GetGuidKey());
        await sendDataGrain.Send(this.GetGrainId().GetGuidKey(), SendBackData);
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request)
    {
        _logger.LogError("This should not be called.");
    }

}


/// <summary>
/// 세션에 연결되어 있는 플레이어의 모든 엔티티를 가지고 있는 상위 그레인,
/// Circular deadlock를 막기 위해 모든 하단 그레인들에 대한 호출 그래프를 관리하다.
/// </summary>
public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    //[GameSpecificProperty]
    //private string name;
    //[GameSpecificProperty]
    //private string displayName;
    //[GameSpecificProperty]
    //private int _level;
    //[GameSpecificProperty]
    //private double _exp;

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


