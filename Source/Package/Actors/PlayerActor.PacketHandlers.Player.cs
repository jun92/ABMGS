using SyncnetPlatform.Extensions;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Actors;

public partial class PlayerActor
{
    [PacketHandler(typeof(Ping))]
    public async Task HandlePing(Ping request)
    {
        if (!_isOnline) return;
        
        PongArgs pongArgs = new(request.Seq + 1);
        byte[] packetToSendBack = SyncnetPacketBuilder.Build(pongArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }

    [PacketHandler(typeof(ReqUserInfo))]
    public async Task HandleReqUserInfo(ReqUserInfo request)
    {
        if (!_isOnline) return;
        
        //byte[] serializedPlayerExtendData = SerializePlayerExtendData();
        byte[] serializedPlayerExtendData = playerCustomBehavior != null ? playerCustomBehavior.Serialize(_playerState.Extension) : [];
        
        ResUserInfoArgs resUserInfoArgs = new(PlayerId, _playerState.PlayerName, serializedPlayerExtendData);
        byte[] packetToSendBack = SyncnetPacketBuilder.Build(resUserInfoArgs);
        await _sendDataGrain.Send(packetToSendBack);
    }

    [PacketHandler(typeof(ReqUpdatePlayerName))]
    public async Task HandleReqUpdatePlayerName(ReqUpdatePlayerName request)
    {
        if (!_isOnline) return;
        
        _playerState.PlayerName = request.PlayerName;
        _isDirtyPlayerData = true;
        ResUpdatePlayerNameArgs resUpdatePlayerNameArgs = new(PacketErrorCodes.Success);
        byte[] packetToSendBack = SyncnetPacketBuilder.Build(resUpdatePlayerNameArgs);
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
                SyncnetPacketBuilder.Build<ResUserActionForUpdatePlayerExtendDataArgs>(
                    new ResUserActionForUpdatePlayerExtendDataArgs(
                        PacketErrorCodes.Success,
                        PacketErrorCodes.Success.ToString(),
                        playerCustomBehavior != null ? playerCustomBehavior.Serialize(_playerState.Extension) : []
                        //SerializePlayerExtendData()
                        )));
        }
        else
        {
            await _sendDataGrain.Send(
                SyncnetPacketBuilder.Build<ResUserActionForUpdatePlayerExtendDataArgs>(
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

        await _sendDataGrain.Send(SyncnetPacketBuilder.Build<ResDirectDeliveryDataArgs>(new ResDirectDeliveryDataArgs(result)));
    }
}
