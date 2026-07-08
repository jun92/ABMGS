using Google.FlatBuffers;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;
using System.Net.Sockets;

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

        VectorOffset customData = ResUserInfo.CreatePlayerCustomVector(builder, args.PlayerCustomData);
        StringOffset playerName = builder.CreateString(args.PlayerName); /*IMPORTANT TO BE CALLED BEFORE START?? FUNCTION*/
        Offset<GuidType> playerId = args.PlayerId.ToGuidType(builder);

        ResUserInfo.StartResUserInfo(builder);
        ResUserInfo.AddPlayerId(builder, playerId);
        ResUserInfo.AddPlayerName(builder, playerName);
        ResUserInfo.AddPlayerCustom(builder, customData);
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

internal class ReqUserActionForUpdatePlayerCustomDataPacketBuilder : PacketBABuilder<ReqUserActionForUpdatePlayerCustomDataArgs>
{
    public override byte[] Build(ReqUserActionForUpdatePlayerCustomDataArgs args)
    {
        var builder = CreateBuilder();
        StringOffset actionTypeOffset = builder.CreateString(args.ActionType);
        VectorOffset actionParametersOffset = ReqUserActionForUpdatePlayerCustomData.CreateActionParameterVector(builder, args.ActionParameters);
        ReqUserActionForUpdatePlayerCustomData.StartReqUserActionForUpdatePlayerCustomData(builder);
        ReqUserActionForUpdatePlayerCustomData.AddActionType(builder, actionTypeOffset);
        ReqUserActionForUpdatePlayerCustomData.AddActionParameter(builder, actionParametersOffset);

        return Wrap(builder,
            SystemPacket.ReqUserActionForUpdatePlayerCustomData,
            ReqUserActionForUpdatePlayerCustomData.EndReqUserActionForUpdatePlayerCustomData(builder).Value);
    }
}

internal class ResUserActionForUpdatePlayerCustomDataPacketBuilder : PacketBABuilder<ResUserActionForUpdatePlayerCustomDataArgs>
{
    public override byte[] Build(ResUserActionForUpdatePlayerCustomDataArgs args)
    {
        var builder = CreateBuilder();
        StringOffset resultOffset = builder.CreateString(args.Message);
        VectorOffset playerCustomVectorOffset = ResUserActionForUpdatePlayerCustomData.CreatePlayerCustomVector(builder, args.updatedPlayerCustom);

        ResUserActionForUpdatePlayerCustomData.StartResUserActionForUpdatePlayerCustomData(builder);
        ResUserActionForUpdatePlayerCustomData.AddResult(builder, args.Result);
        ResUserActionForUpdatePlayerCustomData.AddMessage(builder, resultOffset);
        ResUserActionForUpdatePlayerCustomData.AddPlayerCustom(builder, playerCustomVectorOffset);

        return Wrap(builder,
            SystemPacket.ResUserActionForUpdatePlayerCustomData,
            ResUserActionForUpdatePlayerCustomData.EndResUserActionForUpdatePlayerCustomData(builder).Value);
    }
}

internal class ReqCreateRoomPacketBuilder : PacketBABuilder<ReqCreateRoomArgs>
{
    public override byte[] Build(ReqCreateRoomArgs args)
    {
        var builder = CreateBuilder();
        StringOffset roomName = builder.CreateString(args.name);
        StringOffset roomPassword = builder.CreateString(args.password);
        VectorOffset roomMetaData = ReqCreateRoom.CreateRoomMetadataVector(builder, args.RoomMetadata ?? Array.Empty<byte>());

        ReqCreateRoom.StartReqCreateRoom(builder);
        ReqCreateRoom.AddName(builder, roomName);
        ReqCreateRoom.AddPrivate(builder, args.isPrivate);
        ReqCreateRoom.AddMaxCount(builder, args.maxCount);
        ReqCreateRoom.AddPassword(builder, roomPassword);
        ReqCreateRoom.AddRoomMetadata(builder, roomMetaData);

        return Wrap(builder, SystemPacket.ReqCreateRoom, ReqCreateRoom.EndReqCreateRoom(builder).Value);
    }
}

internal class ResCreateRoomPacketBuilder : PacketBABuilder<ResCreateRoomArgs>
{
    public override byte[] Build(ResCreateRoomArgs args)
    {
        var builder = CreateBuilder();
        ResCreateRoom.StartResCreateRoom(builder);
        ResCreateRoom.AddResult(builder, args.result);
        ResCreateRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        return Wrap(builder, SystemPacket.ResCreateRoom, ResCreateRoom.EndResCreateRoom(builder).Value);
    }
}

internal class ReqJoinRoomPacketBuilder : PacketBABuilder<ReqJoinRoomArgs>
{
    public override byte[] Build(ReqJoinRoomArgs args)
    {
        var builder = CreateBuilder();
        StringOffset roomPassword = builder.CreateString(args.password);
        VectorOffset roomMetadata = ReqJoinRoom.CreateRoomMetadataVector(builder, args.roomMetadata ?? Array.Empty<byte>());
        ReqJoinRoom.StartReqJoinRoom(builder);
        ReqJoinRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        ReqJoinRoom.AddPassword(builder, roomPassword);
        ReqJoinRoom.AddRoomMetadata(builder, roomMetadata);
        return Wrap(builder, SystemPacket.ReqJoinRoom, ReqJoinRoom.EndReqJoinRoom(builder).Value);
    }
}

internal class ResJoinRoomPacketBuilder : PacketBABuilder<ResJoinRoomArgs>
{
    public override byte[] Build(ResJoinRoomArgs args)
    {
        var builder = CreateBuilder();
        return Wrap(
            builder, 
            SystemPacket.ResJoinRoom, 
            ResJoinRoom.CreateResJoinRoom(builder, args.result).Value);
    }
}

internal class OnPlayerJoinRoomPacketBuilder : PacketBABuilder<OnPlayerJoinRoomArgs>
{
    public override byte[] Build(OnPlayerJoinRoomArgs args)
    {
        var builder = CreateBuilder();
        StringOffset playerName = builder.CreateString(args.playerName);
        VectorOffset playerCustomData = OnPlayerJoinRoom.CreatePlayerCustomStateVector(builder, args.PlayerCustomData ?? Array.Empty<byte>());
        
        OnPlayerJoinRoom.StartOnPlayerJoinRoom(builder);
        OnPlayerJoinRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        OnPlayerJoinRoom.AddPlayerId(builder, args.playerId.ToGuidType(builder));
        OnPlayerJoinRoom.AddName(builder, playerName);
        OnPlayerJoinRoom.AddPlayerCustomState(builder, playerCustomData);

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

            PlayerInfoInRoom.StartPlayerInfoInRoom(builder);
            PlayerInfoInRoom.AddPlayerName(builder, PlayerName);
            PlayerInfoInRoom.AddPlayerId(builder, i.playerId.ToGuidType(builder));
            offset.Add(PlayerInfoInRoom.EndPlayerInfoInRoom(builder));
        }
        VectorOffset members = ResPlayerListInRoom.CreateMembersVector(builder,offset.ToArray());


        ResPlayerListInRoom.StartResPlayerListInRoom(builder);
        ResPlayerListInRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        ResPlayerListInRoom.AddMembers(builder, members);

        return Wrap(builder, SystemPacket.ResPlayerListInRoom, ResPlayerListInRoom.EndResPlayerListInRoom(builder).Value);
    }
}

internal class ReqLeaveRoomPacketBuilder : PacketBABuilder<ReqLeaveRoomArgs>
{
    public override byte[] Build(ReqLeaveRoomArgs args)
    {
        var builder = CreateBuilder();
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
internal class DeliverCustomPacketBuilder : PacketBABuilder<DeliverCustomPacketArgs>
{
    public override byte[] Build(DeliverCustomPacketArgs args)
    {
        var builder = CreateBuilder();
        
        VectorOffset customPacketVectorOffset = DeliverCustomPacket.CreateCustomPacketVector(builder, args.CustomData);
        DeliverCustomPacket.StartDeliverCustomPacket(builder);
        DeliverCustomPacket.AddDestination(builder, args.Dest);
        DeliverCustomPacket.AddCustomPacket(builder, customPacketVectorOffset);

        return Wrap(builder, SystemPacket.DeliverCustomPacket, DeliverCustomPacket.EndDeliverCustomPacket(builder).Value);
    }
}