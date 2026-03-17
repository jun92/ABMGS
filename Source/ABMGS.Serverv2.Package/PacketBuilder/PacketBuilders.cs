using Google.FlatBuffers;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;

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
        StringOffset playerName = builder.CreateString(args.PlayerName); /*IMPORTANT TO BE CALLED BEFORE START?? FUNCTION*/
        Offset<GuidType> playerId = args.PlayerId.ToGuidType(builder);

        ResUserInfo.StartResUserInfo(builder);
        ResUserInfo.AddPlayerId(builder, playerId);
        ResUserInfo.AddPlayerName(builder, playerName);
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
            ResUpdatePlayerName.CreateResUpdatePlayerName(builder, args.Result, builder.CreateString(args.Message)).Value
            );
    }
}

internal class ReqCreateRoomPacketBuilder : PacketBABuilder<ReqCreateRoomArgs>
{
    public override byte[] Build(ReqCreateRoomArgs args)
    {
        var builder = CreateBuilder();
        ReqCreateRoom.StartReqCreateRoom(builder);
        ReqCreateRoom.AddName(builder, builder.CreateString(args.name));
        ReqCreateRoom.AddPrivate(builder, args.isPrivate);
        ReqCreateRoom.AddMaxCount(builder, args.maxCount);
        ReqCreateRoom.AddPassword(builder, builder.CreateString(args.password));

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
        ResCreateRoom.AddMessage(builder, builder.CreateString(args.message));
        return Wrap(builder, SystemPacket.ResCreateRoom, ResCreateRoom.EndResCreateRoom(builder).Value);
    }
}

internal class ReqJoinRoomPacketBuilder : PacketBABuilder<ReqJoinRoomArgs>
{
    public override byte[] Build(ReqJoinRoomArgs args)
    {
        var builder = CreateBuilder();
        ReqJoinRoom.StartReqJoinRoom(builder);
        ReqJoinRoom.AddRoomId(builder, args.roomId.ToGuidType(builder));
        ReqJoinRoom.AddPassword(builder, builder.CreateString(args.password));
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
            ResJoinRoom.CreateResJoinRoom(builder, args.result, builder.CreateString(args.message)).Value);
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
            ResLeaveRoom.CreateResLeaveRoom(builder, args.result, builder.CreateString(args.message)).Value);
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
            ResBroadcastRoom.CreateResBroadcastRoom(builder, args.result, builder.CreateString(args.message)).Value);
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