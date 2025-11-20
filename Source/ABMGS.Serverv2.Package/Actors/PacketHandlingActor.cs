using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System.Collections.Concurrent;

namespace SyncnetPlatform.Actors;

public interface ISystemPacketHandler
{

}

public class SystemPacketHandlerBase : ISystemPacketHandler
{
    
}

public class SystemPacketHandler : SystemPacketHandlerBase
{
    [PacketHandler(typeof(Pong))]
    public void Handle(Pong p)
    {
       
    }
}

public class PacketHandlingActor : Grain, IPacketHandler, IPacketObserver
{
    private readonly IPacketRouter _routeTable;
    private readonly ILogger<PacketHandlingActor> _logger;
    private readonly ConcurrentQueue<byte[]> _receiveQueue;
    private ObserverManager<IPacketObserver>? _packetObserverManager;
    private readonly ISystemPacketHandler _systemPacketHandler;
    public PacketHandlingActor(ILogger<PacketHandlingActor> logger, IPacketRouter routeTable)
    {
        _logger = logger;
        _routeTable = routeTable;
        _receiveQueue = new ConcurrentQueue<byte[]>();
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
        if (_packetObserverManager != null)
        {
            await _packetObserverManager.Notify(s => s.NewPacketArrived());
        }
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
        await sendDataGrain.Send(SendBackData);
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request)
    {
        _logger.LogError("This should not be called.");
    }
}


