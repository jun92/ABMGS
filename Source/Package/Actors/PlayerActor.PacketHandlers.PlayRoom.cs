using SyncnetPlatform.Extensions;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Actors;
public partial class PlayerActor
{
    [PacketHandler(typeof(ReqCreateRoom))]
    public async Task HandleReqCreateRoom(ReqCreateRoom request)
    {
        if (!_isOnline || _playRoomSession is null) return;
        
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        byte[]? serializedPlayRoomState = null;
        Guid newPlayRoomId = Guid.NewGuid();
        
        (errorCode, serializedPlayRoomState) = await _playRoomSession.CreatePlayRoom(
            newPlayRoomId, request.Name, request.Private, request.MaxCount, request.Password);
        if (errorCode != PacketErrorCodes.Success)
        {
            await _sendDataGrain.Send(
                SyncnetPacketBuilder.Build(new ResCreateRoomArgs(errorCode, newPlayRoomId, [])));
            return; 
        }
        
        // delegating onCreatePlayRoom event.
        playerCustomBehavior?.OnCreatePlayRoom(_playerState, newPlayRoomId, serializedPlayRoomState);
        
        await _sendDataGrain.Send
            (
                SyncnetPacketBuilder.Build<ResCreateRoomArgs>
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
        if (!_isOnline || _playRoomSession is null) return;
        
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);
        
        
        PacketErrorCodes errorCode = PacketErrorCodes.Success;
        (errorCode, byte[] playRoomCustomState) = await _playRoomSession.JoinPlayRoom(roomId);

        await _sendDataGrain.Send(SyncnetPacketBuilder.Build<ResJoinRoomArgs>(
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
        if (!_isOnline || _playRoomSession is null) return;
        
        
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);
        List<PlayRoomMember> players = await _playRoomSession!.GetPlayerListInPlayRoom(roomId);

        await _sendDataGrain.Send(SyncnetPacketBuilder.Build<ResPlayerListInRoomArgs>(
            new ResPlayerListInRoomArgs(
                roomId, 
                [.. players.Select(s => new PlayerInfoInRoomArgs(s.PlayerId, s.PlayerName, s.PlayerExtendData ??
                    []))]
               )));
    }

    [PacketHandler(typeof(ReqLeaveRoom))]
    public async Task HandleReqLeavePlayRoom(ReqLeaveRoom request)
    {
        if (!_isOnline || _playRoomSession is null) return;
        
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);
        PacketErrorCodes result = await _playRoomSession!.LeavePlayRoom(roomId);

        await _sendDataGrain.Send(SyncnetPacketBuilder.Build<ResLeaveRoomArgs>(
            new ResLeaveRoomArgs(result)
            ));
    }
    
    [PacketHandler(typeof(ReqPlayerActionToPlayRoom))]
    public async Task HandleReqPlayerActionToPlayRoom(ReqPlayerActionToPlayRoom request)
    {
        if (!_isOnline || _playRoomSession is null) return;
        
        Guid roomId = Guid.Empty;
        roomId.FromGuidType(request.RoomId);

        PacketErrorCodes errorCode = await _playRoomSession!.PlayerActionToPlayRoom(roomId, PlayerId, request.ActionType,
            request.GetActionParameterArray());
        ResPlayerActionToPlayRoomArgs resPlayerActionToPlayRoomArgs = new(errorCode, 0);
        byte[] packetToSendBack = SyncnetPacketBuilder.Build(resPlayerActionToPlayRoomArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }
}
