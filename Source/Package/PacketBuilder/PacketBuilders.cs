using Google.FlatBuffers;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Numerics;

namespace SyncnetPlatform.Network.Utils;

public abstract class PacketBABuilder<ArgsType> : IPacketByteArrayBuilder<ArgsType> where ArgsType : IPacketBuildArgs
{
    internal FlatBufferBuilder CreateBuilder() => new (4096);
    public abstract byte[] Build(ArgsType args);

    public byte[] Wrap(FlatBufferBuilder builder, SystemPacket packetType, int offsetValue)
    {
        Offset<PacketWrapper> packetWrapperOffset = PacketWrapper.CreatePacketWrapper(
            builder,
            packetType,
            offsetValue);
        builder.Finish(packetWrapperOffset.Value);
        
        return builder.SizedByteArray();
    }
}

internal class PongPacketBuilder : PacketBABuilder<PongArgs>
{
    public override byte[] Build(PongArgs args)
    {
        var builder = CreateBuilder();

        Offset<Pong> pongOffset = Pong.CreatePong(builder, args.Seq);

        return Wrap(builder, SystemPacket.Pong, pongOffset.Value);
    }

}

internal class PingPacketBuilder : PacketBABuilder<PingArgs>
{
    public override byte[] Build(PingArgs args)
    {
        var builder = CreateBuilder();
        Offset<Ping> pingOffset = Ping.CreatePing(builder, args.Seq);

        return Wrap(builder, SystemPacket.Ping, pingOffset.Value);
    }
}

internal class ReqUserInfoPacketBuilder: PacketBABuilder<ReqUserInfoArgs>
{
    public override byte[] Build(ReqUserInfoArgs args)
    {
        var builder = CreateBuilder();
        ReqUserInfo.StartReqUserInfo(builder);
        Offset<ReqUserInfo> offsetUserInfo = ReqUserInfo.EndReqUserInfo(builder); 
        return Wrap(builder, SystemPacket.ReqUserInfo, offsetUserInfo.Value);
    }
}

internal class ResUserInfoPacketBuilder: PacketBABuilder<ResUserInfoArgs>
{
    public override byte[] Build(ResUserInfoArgs args)
    {
        var builder = CreateBuilder();

        VectorOffset extendData = ResUserInfo.CreateExtendDataVector(builder, args.PlayerExtendData);
        StringOffset playerName = builder.CreateString(args.PlayerName); /*IMPORTANT TO BE CALLED BEFORE START?? FUNCTION*/
        Offset<GuidType> playerId = args.PlayerId.ToGuidType(builder);

        ResUserInfo.StartResUserInfo(builder);
        ResUserInfo.AddId(builder, playerId);
        ResUserInfo.AddName(builder, playerName);
        ResUserInfo.AddExtendData(builder, extendData);
        Offset<ResUserInfo> offsetUserInfo = ResUserInfo.EndResUserInfo(builder);

        return Wrap(builder, SystemPacket.ResUserInfo, offsetUserInfo.Value);
    }
}

internal class ReqUpdatePlayerNamePacketBuilder: PacketBABuilder<ReqUpdatePlayerNameArgs>
{
    public override byte[] Build(ReqUpdatePlayerNameArgs args)
    {
        var builder = CreateBuilder();
        return Wrap(builder,
            SystemPacket.ReqUpdatePlayerName,
            ReqUpdatePlayerName.CreateReqUpdatePlayerName(builder, builder.CreateString(args.PlayerName)).Value
            );
    }
}

internal class ResUpdatePlayerNamePacketBuilder : PacketBABuilder<ResUpdatePlayerNameArgs>
{
    public override byte[] Build(ResUpdatePlayerNameArgs args)
    {
        var builder = CreateBuilder();
        return Wrap(builder,
            SystemPacket.ResUpdatePlayerName,
            ResUpdatePlayerName.CreateResUpdatePlayerName(builder, args.Result).Value
            );
    }
}

internal class ReqUserActionForUpdatePlayerExtendDataPacketBuilder : PacketBABuilder<ReqUserActionForUpdatePlayerExtendDataArgs>
{
    public override byte[] Build(ReqUserActionForUpdatePlayerExtendDataArgs args)
    {
        var builder = CreateBuilder();
        StringOffset actionTypeOffset = builder.CreateString(args.ActionType);
        VectorOffset actionParametersOffset = ReqUserActionForUpdatePlayerExtendData.CreateActionParameterVector(builder, args.ActionParameters);
        ReqUserActionForUpdatePlayerExtendData.StartReqUserActionForUpdatePlayerExtendData(builder);
        ReqUserActionForUpdatePlayerExtendData.AddActionType(builder, actionTypeOffset);
        ReqUserActionForUpdatePlayerExtendData.AddActionParameter(builder, actionParametersOffset);

        return Wrap(builder,
            SystemPacket.ReqUserActionForUpdatePlayerExtendData,
            ReqUserActionForUpdatePlayerExtendData.EndReqUserActionForUpdatePlayerExtendData(builder).Value);
    }
}

internal class ResUserActionForUpdatePlayerExtendDataPacketBuilder : PacketBABuilder<ResUserActionForUpdatePlayerExtendDataArgs>
{
    public override byte[] Build(ResUserActionForUpdatePlayerExtendDataArgs args)
    {
        var builder = CreateBuilder();
        StringOffset resultOffset = builder.CreateString(args.Message);
        VectorOffset playerCustomVectorOffset = ResUserActionForUpdatePlayerExtendData.CreateExtendDataVector(builder, args.updatedPlayerExtendData);

        ResUserActionForUpdatePlayerExtendData.StartResUserActionForUpdatePlayerExtendData(builder);
        ResUserActionForUpdatePlayerExtendData.AddResult(builder, args.Result);
        ResUserActionForUpdatePlayerExtendData.AddMessage(builder, resultOffset);
        ResUserActionForUpdatePlayerExtendData.AddExtendData(builder, playerCustomVectorOffset);

        return Wrap(builder,
            SystemPacket.ResUserActionForUpdatePlayerExtendData,
            ResUserActionForUpdatePlayerExtendData.EndResUserActionForUpdatePlayerExtendData(builder).Value);
    }
}

internal class ReqCreateRoomPacketBuilder : PacketBABuilder<ReqCreateRoomArgs>
{
    public override byte[] Build(ReqCreateRoomArgs args)
    {
        var builder = CreateBuilder();
        StringOffset roomName = builder.CreateString(args.name);
        StringOffset roomPassword = builder.CreateString(args.password);

        ReqCreateRoom.StartReqCreateRoom(builder);
        ReqCreateRoom.AddName(builder, roomName);
        ReqCreateRoom.AddPrivate(builder, args.isPrivate);
        ReqCreateRoom.AddMaxCount(builder, args.maxCount);
        ReqCreateRoom.AddPassword(builder, roomPassword);

        return Wrap(builder, SystemPacket.ReqCreateRoom, ReqCreateRoom.EndReqCreateRoom(builder).Value);
    }
}

internal class ResCreateRoomPacketBuilder : PacketBABuilder<ResCreateRoomArgs>
{
    public override byte[] Build(ResCreateRoomArgs args)
    {
        var builder = CreateBuilder();

        VectorOffset roomState = ResCreateRoom.CreateRoomStateVector(builder, args.RoomState ?? Array.Empty<byte>());
        ResCreateRoom.StartResCreateRoom(builder);
        ResCreateRoom.AddResult(builder, args.result);
        ResCreateRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        ResCreateRoom.AddRoomState(builder, roomState);
        return Wrap(builder, SystemPacket.ResCreateRoom, ResCreateRoom.EndResCreateRoom(builder).Value);
    }
}

internal class ReqJoinRoomPacketBuilder : PacketBABuilder<ReqJoinRoomArgs>
{
    public override byte[] Build(ReqJoinRoomArgs args)
    {
        var builder = CreateBuilder();
        
        StringOffset roomPassword = builder.CreateString(args.password);
        
        ReqJoinRoom.StartReqJoinRoom(builder);
        ReqJoinRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        ReqJoinRoom.AddPassword(builder, roomPassword);
        return Wrap(builder, SystemPacket.ReqJoinRoom, ReqJoinRoom.EndReqJoinRoom(builder).Value);
    }
}

internal class ResJoinRoomPacketBuilder : PacketBABuilder<ResJoinRoomArgs>
{
    public override byte[] Build(ResJoinRoomArgs args)
    {
        var builder = CreateBuilder();
        VectorOffset roomStateOffset = ResJoinRoom.CreateRoomStateVector(builder, args.roomState);
        ResJoinRoom.StartResJoinRoom(builder);
        ResJoinRoom.AddResult(builder, args.result);
        ResJoinRoom.AddAppErrorCode(builder, args.AppErrorCode);
        ResJoinRoom.AddRoomState(builder, roomStateOffset);
        
        return Wrap(
            builder, 
            SystemPacket.ResJoinRoom, 
            ResJoinRoom.EndResJoinRoom(builder).Value);
    }
}

internal class OnPlayerJoinRoomPacketBuilder : PacketBABuilder<OnPlayerJoinRoomArgs>
{
    public override byte[] Build(OnPlayerJoinRoomArgs args)
    {
        var builder = CreateBuilder();
        StringOffset playerName = builder.CreateString(args.playerName);
        VectorOffset playerCustomData = OnPlayerJoinRoom.CreateJoinerMetadataVector(builder, args.PlayerMetadata ?? []);
        
        OnPlayerJoinRoom.StartOnPlayerJoinRoom(builder);
        OnPlayerJoinRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        OnPlayerJoinRoom.AddJoinerId(builder, args.playerId.ToGuidType(builder));
        OnPlayerJoinRoom.AddJoinerName(builder, playerName);
        OnPlayerJoinRoom.AddJoinerMetadata(builder, playerCustomData);

        return Wrap(
            builder,
            SystemPacket.OnPlayerJoinRoom, 
            OnPlayerJoinRoom.EndOnPlayerJoinRoom(builder).Value);
    }
}

internal class ReqPlayerListInRoomPacketBuilder : PacketBABuilder<ReqPlayerListInRoomArgs>
{
    public override byte[] Build(ReqPlayerListInRoomArgs args)
    {
        var builder = CreateBuilder();
        ReqPlayerListInRoom.StartReqPlayerListInRoom(builder);
        ReqPlayerListInRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        return Wrap(builder, SystemPacket.ReqPlayerListInRoom, ReqPlayerListInRoom.EndReqPlayerListInRoom(builder).Value);
    }
}

internal class ResPlayerListInRoomPacketBuilder : PacketBABuilder<ResPlayerListInRoomArgs>
{
    public override byte[] Build(ResPlayerListInRoomArgs args)
    {
        var builder = CreateBuilder();
        List<Offset<PlayerInfoInRoom>> offset = new ();
        foreach(var i in args.playerInfo)
        {
            StringOffset PlayerName = builder.CreateString(i.playerName);
            VectorOffset PlayerExtendData = PlayerInfoInRoom.CreateExtendDataVector(builder, i.PlayerExtendData);

            PlayerInfoInRoom.StartPlayerInfoInRoom(builder);
            PlayerInfoInRoom.AddId(builder, i.playerId.ToGuidType(builder));
            PlayerInfoInRoom.AddName(builder, PlayerName);
            PlayerInfoInRoom.AddExtendData(builder, PlayerExtendData);
            offset.Add(PlayerInfoInRoom.EndPlayerInfoInRoom(builder));
        }
        VectorOffset members = ResPlayerListInRoom.CreateMembersVector(builder,offset.ToArray());


        ResPlayerListInRoom.StartResPlayerListInRoom(builder);
        ResPlayerListInRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        ResPlayerListInRoom.AddMembers(builder, members);

        return Wrap(builder, SystemPacket.ResPlayerListInRoom, ResPlayerListInRoom.EndResPlayerListInRoom(builder).Value);
    }
}

internal class OnPlayRoomUpdatePacketBuilder : PacketBABuilder<OnPlayRoomStateUpdateArgs>
{
    public override byte[] Build(OnPlayRoomStateUpdateArgs args)
    {
        var builder = CreateBuilder();

        VectorOffset updatePlayRoomState = OnPlayRoomStateUpdate.CreateUpdatedRoomStateVector(builder, args.UpdatedPlayRoomState??[]);
        OnPlayRoomStateUpdate.StartOnPlayRoomStateUpdate(builder);
        OnPlayRoomStateUpdate.AddRoomId(builder, args.RoomId.ToGuidType(builder));
        OnPlayRoomStateUpdate.AddUpdatedRoomState(builder, updatePlayRoomState);
        return Wrap(builder, SystemPacket.OnPlayRoomStateUpdate, OnPlayRoomStateUpdate.EndOnPlayRoomStateUpdate(builder).Value);
    }
}

internal class OnPlayRoomUpdatePlayerExtendDataPacketBuilder : PacketBABuilder<OnPlayRoomUpdatePlayerExtendDataArgs>
{
    public override byte[] Build(OnPlayRoomUpdatePlayerExtendDataArgs args)
    {
        var builder = CreateBuilder();
        VectorOffset updatedPlayerExtendData = OnPlayRoomUpdatePlayerExtendData.CreateUpdatedPlayerExtendDataVectorBlock(builder, args.UpdatePlayerExtendData??[]);
        OnPlayRoomUpdatePlayerExtendData.StartOnPlayRoomUpdatePlayerExtendData(builder);
        OnPlayRoomUpdatePlayerExtendData.AddPlayerId(builder, args.PlayerId.ToGuidType(builder));
        OnPlayRoomUpdatePlayerExtendData.AddUpdatedPlayerExtendData(builder, updatedPlayerExtendData);
        return Wrap(builder, SystemPacket.OnPlayRoomUpdatePlayerExtendData, OnPlayRoomUpdatePlayerExtendData.EndOnPlayRoomUpdatePlayerExtendData(builder).Value);
    }
}

internal class ReqPlayerActionToPlayRoomPacketBuilder : PacketBABuilder<ReqPlayerActionToPlayRoomArgs>
{
    public override byte[] Build(ReqPlayerActionToPlayRoomArgs args)
    {
        var builder = CreateBuilder();
        VectorOffset actionParameter = ReqPlayerActionToPlayRoom.CreateActionParameterVector(builder, args.ActionParameter??[]);
        StringOffset actionType = builder.CreateString(args.ActionType);
        ReqPlayerActionToPlayRoom.StartReqPlayerActionToPlayRoom(builder);
        ReqPlayerActionToPlayRoom.AddRoomId(builder, args.RoomId.ToGuidType(builder));
        ReqPlayerActionToPlayRoom.AddActionType(builder, actionType);
        ReqPlayerActionToPlayRoom.AddActionParameter(builder, actionParameter);
        return Wrap(builder, SystemPacket.ReqPlayerActionToPlayRoom, ReqPlayerActionToPlayRoom.EndReqPlayerActionToPlayRoom(builder).Value);
    }
}

internal class ResPlayerActionToPlayRoomPacketBuilder : PacketBABuilder<ResPlayerActionToPlayRoomArgs>
{
    public override byte[] Build(ResPlayerActionToPlayRoomArgs args)
    {
        var builder = CreateBuilder();
        ResPlayerActionToPlayRoom.StartResPlayerActionToPlayRoom(builder);
        ResPlayerActionToPlayRoom.AddResult(builder, args.result);
        ResPlayerActionToPlayRoom.AddAppErrorCode(builder, args.app_error_code);
        return Wrap(builder, SystemPacket.ResPlayerActionToPlayRoom, ResPlayerActionToPlayRoom.EndResPlayerActionToPlayRoom(builder).Value);
    }
}

internal class OnPlayerActionToPlayRoomResultPacketBuilder : PacketBABuilder<OnPlayerActionToPlayRoomResultArgs>
{
    public override byte[] Build(OnPlayerActionToPlayRoomResultArgs args)
    {
        FlatBufferBuilder builder = CreateBuilder();
        VectorOffset actionParameterResult =
            OnPlayerActionToPlayRoomResult.CreateActionParameterResultVector(builder,args.ActionParameterResult);
        StringOffset actionType = builder.CreateString(args.ActionType);
        OnPlayerActionToPlayRoomResult.StartOnPlayerActionToPlayRoomResult(builder);
        OnPlayerActionToPlayRoomResult.AddActionType(builder, actionType);
        OnPlayerActionToPlayRoomResult.AddActionParameterResult(builder, actionParameterResult);
        return Wrap(builder, SystemPacket.OnPlayerActionToPlayRoomResult,  OnPlayerActionToPlayRoomResult.EndOnPlayerActionToPlayRoomResult(builder).Value);
    }
}

internal class ReqLeaveRoomPacketBuilder : PacketBABuilder<ReqLeaveRoomArgs>
{
    public override byte[] Build(ReqLeaveRoomArgs args)
    {
        FlatBufferBuilder builder = CreateBuilder();
        ReqLeaveRoom.StartReqLeaveRoom(builder);
        ReqLeaveRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        return Wrap(builder, SystemPacket.ReqLeaveRoom, ReqLeaveRoom.EndReqLeaveRoom(builder).Value);
    }
}

internal class ResLeaveRoomPacketBuilder : PacketBABuilder<ResLeaveRoomArgs>
{
    public override byte[] Build(ResLeaveRoomArgs args)
    {
        var builder = CreateBuilder();
        return Wrap(
            builder, 
            SystemPacket.ResLeaveRoom, 
            ResLeaveRoom.CreateResLeaveRoom(builder, args.result).Value);
    }
}

internal class OnPlayerLeaveRoomPacketBuilder : PacketBABuilder<OnPlayerLeaveRoomArgs>
{
    public override byte[] Build(OnPlayerLeaveRoomArgs args)
    {
        var builder = CreateBuilder();
        StringOffset leaverName = builder.CreateString(args.playerName);
        OnPlayerLeaveRoom.StartOnPlayerLeaveRoom(builder);
        OnPlayerLeaveRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        OnPlayerLeaveRoom.AddPlayerId(builder, args.playerId.ToGuidType(builder));
        OnPlayerLeaveRoom.AddName(builder, leaverName);

        return Wrap(builder, SystemPacket.OnPlayerLeaveRoom, OnPlayerLeaveRoom.EndOnPlayerLeaveRoom(builder).Value);
    }
}

internal class ReqBroadcastRoomPacketBuilder : PacketBABuilder<ReqBroadcastRoomArgs>
{
    public override byte[] Build(ReqBroadcastRoomArgs args)
    {
        var builder = CreateBuilder();

        Offset<GuidType> roomId = args.roomId.ToGuidType(builder);

        ReqBroadcastRoom.StartReqBroadcastRoom(builder);
        ReqBroadcastRoom.AddRoomId(builder, roomId);
        ReqBroadcastRoom.AddFrom(builder, args.from.ToGuidType(builder));
        ReqBroadcastRoom.AddMessage(builder, builder.CreateString(args.message));

        return Wrap(builder, SystemPacket.ReqBroadcastRoom, ReqBroadcastRoom.EndReqBroadcastRoom(builder).Value);
    }
}

internal class ResBroadcastRoomPacketBuilder : PacketBABuilder<ResBroadcastRoomArgs>
{
    public override byte[] Build(ResBroadcastRoomArgs args)
    {
        var builder = CreateBuilder();

        return Wrap(builder,
            SystemPacket.ResBroadcastRoom,
            ResBroadcastRoom.CreateResBroadcastRoom(builder, args.result).Value);
    }
}

internal class BroadcastRoomPacketBuilder : PacketBABuilder<BroadcastRoomArgs>
{
    public override byte[] Build(BroadcastRoomArgs args)
    {
        var builder = CreateBuilder();
        BroadcastRoom.StartBroadcastRoom(builder);
        BroadcastRoom.AddFrom(builder, args.from.ToGuidType(builder));
        BroadcastRoom.AddMessage(builder, builder.CreateString(args.message));
        return Wrap(builder, SystemPacket.BroadcastRoom, BroadcastRoom.EndBroadcastRoom(builder).Value);
    }
}
