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
using System.Threading.Channels;
using System.Diagnostics;

namespace SyncnetPlatform.Actors;

public class PacketHandlingActor : Grain, IPacketHandlerActor
{
    private static readonly ActivitySource TraceSource = new("syncnet.traces");

    private readonly struct PendingPacket
    {
        public byte[] Data { get; }
        public Activity? QueueActivity { get; }

        public PendingPacket(byte[] data, Activity? queueActivity)
        {
            Data = data;
            QueueActivity = queueActivity;
        }
    }

    private readonly IPacketRouter _routeTable;
    private readonly ILogger<PacketHandlingActor> _logger;
    private readonly Channel<PendingPacket> _receiveQueueChannel;
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
        _receiveQueueChannel = Channel.CreateBounded<PendingPacket>(new BoundedChannelOptions(150)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
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
        _receiveQueueChannel.Writer.TryComplete();
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

    public async Task PushRecievedData(byte[] Data)
    {
        var queueActivity = TraceSource.StartActivity("QueueResidenceTime", ActivityKind.Internal);
        await _receiveQueueChannel.Writer.WriteAsync(new PendingPacket(Data, queueActivity));
    }

    public async Task RunRoutingPackets(CancellationToken shutdownToken)
    {
        try
        {
            await foreach (var pending in _receiveQueueChannel.Reader.ReadAllAsync(shutdownToken))
            {
                pending.QueueActivity?.Dispose();

                using var handleActivity = TraceSource.StartActivity(
                    "HandlePacketLogic", 
                    ActivityKind.Internal, 
                    parentContext: pending.QueueActivity?.Context ?? default);

                await InvokeHandler(pending.Data);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RunRoutingPackets loop");
        }
    }
}

