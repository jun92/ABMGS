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
using SyncnetPlatform.Utils.Telemetry;
using System.Diagnostics;
using System.Threading;

namespace SyncnetPlatform.Actors;

public partial class PlayerActor
{
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        if (!_IsOnline) return;
        
        PongArgs pongArgs = new(request.Seq + 1);
        byte[] packetToSendBack = PacketBuilder.Build(pongArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }

    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request)
    {
        if (!_IsOnline) return;
        
        byte[] serializedPlayerExtendData = SerializePlayerExtendData();
        ResUserInfoArgs resUserInfoArgs = new(PlayerId, _playerState.PlayerName, serializedPlayerExtendData);
        byte[] packetToSendBack = PacketBuilder.Build(resUserInfoArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request)
    {
        if (!_IsOnline) return;
        
        _playerState.PlayerName = request.PlayerName;
        _IsDirtyPlayerData = true;
        ResUpdatePlayerNameArgs resUpdatePlayerNameArgs = new(PacketErrorCodes.Success);
        byte[] packetToSendBack = PacketBuilder.Build(resUpdatePlayerNameArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }
    
    [PacketHandler(typeof(ReqUserActionForUpdatePlayerExtendData))]
    public async Task HandleReqUserActionForUpdatePlayerCustomData(ReqUserActionForUpdatePlayerExtendData request)
    {
        if(_playerCustomBehavior is not null)
        {
            _playerCustomBehavior.UpdatePlayerExtendDataByUserAction(
                request.ActionType,
                request.GetActionParameterArray(),
                _playerState
                );
            _IsDirtyPlayerData = true;
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

    [PacketHandler(typeof(ReqCreateRoom))]
    public async Task HandleReqCreateRoom(ReqCreateRoom request)
    {
        
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        byte[]? serializedPlayRoomState = null;
        Guid newPlayRoomId = Guid.NewGuid();
        
        
        
        (errorCode, serializedPlayRoomState) = await _playRoomComponent.CreatePlayRoom(
            newPlayRoomId, request.Name, request.Private, request.MaxCount, request.Password);
        if (errorCode != PacketErrorCodes.Success)
        {
            await _sendDataGrain.Send(
                PacketBuilder.Build(new ResCreateRoomArgs(errorCode, newPlayRoomId, [])));
            return; 
        }
        
        // delegating onCreatePlayRoom event.
        _playerCustomBehavior?.OnCreatePlayRoom(_playerState, newPlayRoomId, serializedPlayRoomState);
        
        await _sendDataGrain.Send
            (
                PacketBuilder.Build<ResCreateRoomArgs>
                (
                    new ResCreateRoomArgs(
                        errorCode, 
                        newPlayRoomId, 
                        serializedPlayRoomState ?? [])
                )
            );
    }

    [PacketHandler(typeof(ReqJoinRoom))]
    public async Task HandleReqJoinRoom(ReqJoinRoom request)
    {
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);
        
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        (errorCode, byte[] playRoomCustomState) = await _playRoomComponent.JoinPlayRoom(roomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResJoinRoomArgs>(
            new ResJoinRoomArgs(
                errorCode, 
                0, 
                playRoomCustomState)
            )
            );
    }

    [PacketHandler(typeof(ReqPlayerListInRoom))]
    public async Task HandleReqPlayerListInRoom(ReqPlayerListInRoom request)
    {
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);
        List<PlayRoomMember> players = await _playRoomComponent.GetPlayerListInPlayRoom(roomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResPlayerListInRoomArgs>(
            new ResPlayerListInRoomArgs(
                roomId, 
                [.. players.Select(s => new PlayerInfoInRoomArgs(s.PlayerId, s.PlayerName, s.PlayerExtendData ??
                    []))]
               )));
    }

    [PacketHandler(typeof(ReqLeaveRoom))]
    public async Task HandleReqLeavePlayRoom(ReqLeaveRoom request)
    {
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);
        PacketErrorCodes result = await _playRoomComponent.LeavePlayRoom(roomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResLeaveRoomArgs>(
            new ResLeaveRoomArgs(result)
            ));
    }
    
    [PacketHandler(typeof(ReqPlayerActionToPlayRoom))]
    public async Task HandleReqPlayerActionToPlayRoom(ReqPlayerActionToPlayRoom request)
    {
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);

        if (!_joinedRoomList.Contains(roomId))
        {
            ResPlayerActionToPlayRoomArgs packetArgs = new (PacketErrorCodes.YoureNotInTheRoom, 0);
            byte[] sendData = PacketBuilder.Build(packetArgs);
            await _sendDataGrain.Send(sendData);
            return;
        }

        IPlayRoomActor playRoomActor = GrainFactory.GetGrain<IPlayRoomActor>(roomId);
        
        await playRoomActor.OnPlayerActionToPlayRoom(
            this.GetGrainId().GetGuidKey(), 
            request.ActionType, 
            request.GetActionParameterArray());
    }
    
    public async ValueTask InvokeHandler(byte[] data)
    {
        await _routeTable.Execute(
            PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data)));
    }

    public async ValueTask PushRecievedData(byte[] Data)
    {
        var currentActivity = Activity.Current;
        Activity.Current = null;
        try
        {
            var queueActivity = SyncnetTelemetry.Trace.StartActivity("InReceiveQueue", ActivityKind.Internal);
            await _receiveQueueChannel.Writer.WriteAsync(new PendingPacket(Data, queueActivity));
        }
        finally
        {
            Activity.Current = currentActivity;
        }
    }

    public async Task RunRoutingPackets(CancellationToken shutdownToken)
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
            _logger.LogError(ex, "Error in RunRoutingPackets loop");
        }
    }
}
