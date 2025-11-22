using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Utils;

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
public class PacketHandlingActor : Grain, IPacketHandler
{
    private readonly IPacketRouter _routeTable;
    private readonly ILogger<PacketHandlingActor> _logger;
    private readonly QueueWithTCS<byte[]> _receiveQueue;
    private CancellationTokenSource? _ctsForRunRoutingPackets;
    private Task? _runRoutingPackets;
    public PacketHandlingActor(
        ILogger<PacketHandlingActor> logger, 
        IPacketRouter routeTable)
    {
        _logger = logger;
        _routeTable = routeTable;
        _receiveQueue = new QueueWithTCS<byte[]>();
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _ctsForRunRoutingPackets = new CancellationTokenSource();

        _runRoutingPackets =  RunRoutingPackets(_ctsForRunRoutingPackets.Token);

        _routeTable.BuildParamExtractionFuncs<PacketWrapper>();
        _routeTable.BuildPacketHandlerFunctions<PacketHandlingActor>(this);
        return Task.CompletedTask;
    }
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _ctsForRunRoutingPackets?.Cancel();
        _receiveQueue.Enqueue(Array.Empty<byte>());
        if( _runRoutingPackets != null) await _runRoutingPackets;
    }

    public Task InvokeHandler(byte[] data)
    {
        _routeTable.Execute(PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data)));
        return Task.CompletedTask;
    }

    public Task PushRecievedData(byte[] Data)
    {
        _receiveQueue.Enqueue(Data);
        return Task.CompletedTask;
    }
    public async Task RunRoutingPackets(CancellationToken shutdownToken)
    {
        while(!shutdownToken.IsCancellationRequested)
        {
            var data = await _receiveQueue.DequeueAsync();
            await InvokeHandler(data);
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

