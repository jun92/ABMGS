using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
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

namespace SyncnetPlatform.Actors;

public partial class PlayerActor
{
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        await PingPong(request.Seq);
    }

    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request)
    {
        string playerName = await GetPlayerName();
        byte[] serializedCustomData = SerializePlayerExtendData();
        await _sendDataGrain.Send(PacketBuilder.Build<ResUserInfoArgs>(new ResUserInfoArgs(PlayerId, playerName, serializedCustomData)));
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request)
    {
        await UpdatePlayerName(request.PlayerName);
        await _sendDataGrain.Send(PacketBuilder.Build<ResUpdatePlayerNameArgs>(new ResUpdatePlayerNameArgs(PacketErrorCodes.Success)));
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
        Guid toPlayerId = default;
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
        
        (PacketErrorCodes errorCode, Guid roomId, byte[]? playRoomCustomState) = await CreateAndJoinPlayRoom(
            request.Name, 
            request.Private, 
            request.MaxCount, 
            request.Password,
            SerializePlayerExtendData());
        
        await _sendDataGrain.Send
            (
                PacketBuilder.Build<ResCreateRoomArgs>
                (
                    new ResCreateRoomArgs(
                        errorCode, 
                        roomId, 
                        playRoomCustomState ?? [])
                )
            );
    }

    [PacketHandler(typeof(ReqJoinRoom))]
    public async Task HandleReqJoinRoom(ReqJoinRoom request)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        var (resultCode, playRoomCustomState) = await JoinPlayRoom(RoomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResJoinRoomArgs>(
            new ResJoinRoomArgs(
                resultCode, 
                0, 
                playRoomCustomState)
            )
            );
    }

    [PacketHandler(typeof(ReqPlayerListInRoom))]
    public async Task HandleReqPlayerListInRoom(ReqPlayerListInRoom request)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        List<PlayRoomMember> Players = await GetPlayerListInPlayRoom(RoomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResPlayerListInRoomArgs>(
            new ResPlayerListInRoomArgs(
                RoomId, 
                [.. Players.Select(s => new PlayerInfoInRoomArgs(s.PlayerId, s.PlayerName, s.PlayerExtendData ?? Array.Empty<byte>()))]
               )));
    }

    [PacketHandler(typeof(ReqLeaveRoom))]
    public async Task HandleReqLeavePlayRoom(ReqLeaveRoom request)
    {
        Guid RoomId = default;
        RoomId.FromGuidType(request.RoomId);
        PacketErrorCodes result = await LeavePlayRoom(RoomId);

        await _sendDataGrain.Send(PacketBuilder.Build<ResLeaveRoomArgs>(
            new ResLeaveRoomArgs(result)
            ));
    }
    [PacketHandler(typeof(DeliverCustomPacket))]
    public async Task HandleDeliverCustomPacket(DeliverCustomPacket request)
    {
        switch(request.Destination)
        {
            case DeliverDestination.None: break;
            case DeliverDestination.Player:
                await OnHandleCustomPacket(request.GetCustomPacketArray());
                break;
            case DeliverDestination.PlayRoom: 
                break;
        }
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
