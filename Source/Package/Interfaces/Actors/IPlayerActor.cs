using Orleans;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Controllers;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncnetPlatform.Interfaces.Actors;

[Alias("SyncnetPlatform.Interfaces.Actors.IPlayerActor")]
public interface IPlayerActor : IGrainWithGuidKey, IPacketHandlerActor
{
    [Alias("OnDirectDeliveryData")]
    Task<PacketErrorCodes> OnDirectDeliveryData(Guid fromPlayerId, string message, DirectDeliveryDataType dataType);
    
    [Alias("OnUpdateForPlayRoomMembers")]
    ValueTask OnUpdateForPlayRoomMembers(PlayRoomMember playRoomMember, PlayRoomMemberUpdateReason memberStatus);

    [Alias("OnUpdatePlayerExtendData")]
    ValueTask OnUpdatePlayerExtendData(byte[] extendData);
    
    [Alias("SendDirectDeliverData")]
    Task<PacketErrorCodes> SendDirectDeliverData(Guid toPlayerId, string message, DirectDeliveryDataType dataType);
    
    [Alias("SetIdProvider")]
    ValueTask SetIdProvider(SupportedPlatformType idpFrom);
    
    [Alias("SetOnline")]
    ValueTask SetOnline(bool isOnline);

    [Alias("OnUpdatePlayRoomCustomState")]
    ValueTask OnUpdatePlayRoomCustomState(Guid roomId, byte[] customState);

    // have given the authority for player stats to somebody(ex: Playroom) 
    // potentially corupt your manual changes to player stats when it is returning true
    [Alias("IsDelegatingPlayerStats")]
    ValueTask<bool> IsDelegatingPlayerStats();

}


