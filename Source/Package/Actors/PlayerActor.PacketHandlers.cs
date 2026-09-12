using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Network.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PacketBuilder = SyncnetPlatform.Network.Utils.SyncnetPacketBuilder;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Utils.Telemetry;
using System.Diagnostics;
using System.Threading;

namespace SyncnetPlatform.Actors;

public partial class PlayerActor
{
    private void SetupNetworkProcessingUnits()
    {
        _sendDataGrain = GrainFactory.GetGrain<ISendDataGrain>(this.GetGrainId().GetGuidKey());
        
        // Keep pumping up packets 
        _ctsForRunRoutingPackets = new CancellationTokenSource();
        _runRoutingPackets = RunRoutingPackets(_ctsForRunRoutingPackets.Token);
        
        routeTable.BuildParamExtractionFuncs<PacketWrapper>();
        routeTable.BuildPacketHandlerFunctions<PlayerActor>(this);
    }
    private async ValueTask InvokeHandler(byte[] data)
    {
        await routeTable.Execute(
            PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data)));
    }

    public async ValueTask PushRecievedData(byte[] data)
    {
        var currentActivity = Activity.Current;
        Activity.Current = null;
        try
        {
            var queueActivity = SyncnetTelemetry.Trace.StartActivity("InReceiveQueue", ActivityKind.Internal);
            await _receiveQueueChannel.Writer.WriteAsync(new PendingPacket(data, queueActivity));
        }
        finally
        {
            Activity.Current = currentActivity;
        }
    }
    private async Task RunRoutingPackets(CancellationToken shutdownToken)
    {
        try
        {
            await foreach (var pending in _receiveQueueChannel.Reader.ReadAllAsync(shutdownToken))
            {
                ActivityContext parentContext = pending.QueueActivity?.Context ?? default;
                pending.QueueActivity?.Dispose();

                using var handleActivity = SyncnetTelemetry.Trace.StartActivity(
                    "HandlePacketLogic", 
                    ActivityKind.Internal,
                    parentContext: parentContext
                );
                await InvokeHandler(pending.Data);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RunRoutingPackets loop");
        }
    }
}
