using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Orleans;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Utils;

namespace SyncnetPlatform.Actors;

public interface IPacketHandler
{

}
public interface ISystemPacketHandler : IPacketHandler
{
}

public interface ICustomPacketHandler : IPacketHandler
{

}

public class SystemPacketHandler : ISystemPacketHandler
{
    private readonly ILogger<SystemPacketHandler> _logger;
    public SystemPacketHandler(ILogger<SystemPacketHandler> logger)
    {
        _logger = logger;
    }

    [PacketHandler(typeof(Dummy))]
    public async Task HandleDummpy(Dummy dummpy, PacketContext ctx)
    {
        _logger.LogError("Dummy packet received. Are you dummy?");
    }


    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request, PacketContext ctx)
    {
        _logger.LogInformation($"HandlePing, Seq is {request.Seq}");
        
        await ctx.SendData(
            ctx.GetPlayerId(),
            SyncnetPacketBuilder.Build(new PongArgs(request.Seq + 1)));
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request, PacketContext ctx)
    {
        _logger.LogError("This should not be called.");
    }
    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request, PacketContext ctx)
    {
        IPlayerActor player = ctx.GetPlayer();
    }
}



public class PacketHandlingActor : Grain, IPacketHandlerActor
{
    private readonly IPacketRouter _routeTable;
    private readonly ILogger<PacketHandlingActor> _logger;
    private readonly QueueWithTCS<byte[]> _receiveQueue;
    private readonly IPacketContextFactory _packetContextFactory;
    private CancellationTokenSource? _ctsForRunRoutingPackets;
    private readonly ISystemPacketHandler _systemPacketHandler;
    private Task? _runRoutingPackets;
    public PacketHandlingActor(
        ILogger<PacketHandlingActor> logger, 
        IPacketRouter routeTable,
        IPacketContextFactory packetContextFactory,
        ISystemPacketHandler systemPacketHandler)
    {
        _logger = logger;
        _routeTable = routeTable;
        _receiveQueue = new QueueWithTCS<byte[]>();
        _packetContextFactory = packetContextFactory;
        _systemPacketHandler = systemPacketHandler;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _ctsForRunRoutingPackets = new CancellationTokenSource();

        _runRoutingPackets =  RunRoutingPackets(_ctsForRunRoutingPackets.Token);

        _routeTable.BuildParamExtractionFuncs<PacketWrapper>();
        _routeTable.BuildPacketHandlerFunctions<ISystemPacketHandler>(_systemPacketHandler);
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
        _routeTable.Execute(
            PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data)), 
            _packetContextFactory.Create(this.GetGrainId().GetGuidKey()));
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
    public async Task HandleDummpy(Dummy dummpy, PacketContext ctx)
    {
        _logger.LogError("Dummy packet received. Are you dummy?");
    }


    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request, PacketContext ctx)
    {
        _logger.LogInformation($"HandlePing, Seq is {request.Seq}");


        byte[] SendBackData = SyncnetPacketBuilder.Build(new PongArgs(request.Seq + 1));

        ISendDataGrain sendDataGrain = GrainFactory.GetGrain<ISendDataGrain>(this.GetGrainId().GetGuidKey());
        await sendDataGrain.Send(SendBackData);
    }
    [PacketHandler(typeof(Pong))]
    public async Task HandlePong(Pong request, PacketContext ctx)
    {
        _logger.LogError("This should not be called.");
    }
}

