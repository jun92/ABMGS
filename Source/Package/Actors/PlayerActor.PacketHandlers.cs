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
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        if (!_isOnline) return;
        
        PongArgs pongArgs = new(request.Seq + 1);
        byte[] packetToSendBack = PacketBuilder.Build(pongArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }

    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request)
    {
        if (!_isOnline) return;
        
        byte[] serializedPlayerExtendData = SerializePlayerExtendData();
        ResUserInfoArgs resUserInfoArgs = new(PlayerId, _playerState.PlayerName, serializedPlayerExtendData);
        byte[] packetToSendBack = PacketBuilder.Build(resUserInfoArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request)
    {
        if (!_isOnline) return;
        
        _playerState.PlayerName = request.PlayerName;
        _isDirtyPlayerData = true;
        ResUpdatePlayerNameArgs resUpdatePlayerNameArgs = new(PacketErrorCodes.Success);
        byte[] packetToSendBack = PacketBuilder.Build(resUpdatePlayerNameArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }
    
    [PacketHandler(typeof(ReqUserActionForUpdatePlayerExtendData))]
    public async Task HandleReqUserActionForUpdatePlayerCustomData(ReqUserActionForUpdatePlayerExtendData request)
    {
        if(playerCustomBehavior is not null)
        {
            playerCustomBehavior.UpdatePlayerExtendDataByUserAction(
                request.ActionType,
                request.GetActionParameterArray(),
                _playerState
                );
            _isDirtyPlayerData = true;
            await _sendDataGrain.Send(
                PacketBuilder.Build<ResUserActionForUpdatePlayerExtendDataArgs>(
                    new ResUserActionForUpdatePlayerExtendDataArgs(
                        PacketErrorCodes.Success,
                        PacketErrorCodes.Success.ToString(),
                        SerializePlayerExtendData()
                        )));
        }
        else
        {
            await _sendDataGrain.Send(
                PacketBuilder.Build<ResUserActionForUpdatePlayerExtendDataArgs>(
                    new ResUserActionForUpdatePlayerExtendDataArgs(
                        PacketErrorCodes.InterfaceNotImplemented,
                        PacketErrorCodes.InterfaceNotImplemented.ToString(),
                        Array.Empty<byte>()
                        )));
        }
    }

    [PacketHandler(typeof(ReqDirectDeliveryData))]
    public async Task HandleReqDirectDeliveryData(ReqDirectDeliveryData request)
    {
        Guid toPlayerId = Guid.Empty;
        toPlayerId.FromGuidType(request.ToPlayerId);

        PacketErrorCodes result = await SendDirectDeliverData(
            toPlayerId,
            request.Data, 
            request.DataType);

        await _sendDataGrain.Send(PacketBuilder.Build<ResDirectDeliveryDataArgs>(new ResDirectDeliveryDataArgs(result)));
    }

    
    
    public async ValueTask InvokeHandler(byte[] data)
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

    private void SetupNetworkProcessingUnits()
    {
        _sendDataGrain = GrainFactory.GetGrain<ISendDataGrain>(this.GetGrainId().GetGuidKey());
        
        // Keep pumping up packets 
        _ctsForRunRoutingPackets = new CancellationTokenSource();
        _runRoutingPackets = RunRoutingPackets(_ctsForRunRoutingPackets.Token);
        
        routeTable.BuildParamExtractionFuncs<PacketWrapper>();
        routeTable.BuildPacketHandlerFunctions<PlayerActor>(this);
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
