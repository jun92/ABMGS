using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Orleans;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Utils;

namespace SyncnetPlatform.Actors;

public class PacketHandlingActor : Grain, IPacketHandlerActor
{
    private readonly IPacketRouter _routeTable;
    private readonly ILogger<PacketHandlingActor> _logger;
    private readonly QueueWithTCS<byte[]> _receiveQueue;
    private readonly IPacketContextFactory _packetContextFactory;
    private CancellationTokenSource? _ctsForRunRoutingPackets;
    private readonly ISystemPacketHandler _systemPacketHandler;
    private Task? _runRoutingPackets;
    private PacketContext? _packetContext = null;
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
        _packetContext = _packetContextFactory.Create(this.GetGrainId().GetGuidKey());

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
        _packetContext = null;
    }

    public async Task InvokeHandler(byte[] data)
    {
        if(_packetContext == null )
        {
            _packetContext = _packetContextFactory.Create(this.GetGrainId().GetGuidKey());
        }
        await _routeTable.Execute(
            PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data)), _packetContext);
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
}

