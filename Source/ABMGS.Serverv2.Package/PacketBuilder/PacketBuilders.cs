using Google.FlatBuffers;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;

public abstract class PacketBABuilder<ArgsType> : IPacketByteArrayBuilder<ArgsType> where ArgsType : IPacketBuildArgs
{
    internal FlatBufferBuilder CreateBuilder() => new FlatBufferBuilder(4096);
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
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        Offset<Pong> pongOffset = Pong.CreatePong(builder, args.Seq);

        return Wrap(builder, SystemPacket.Pong, pongOffset.Value);
    }

}

internal class PingPacketBuilder : PacketBABuilder<PingArgs>
{
    public override byte[] Build(PingArgs args)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        Offset<Ping> pingOffset = Ping.CreatePing(builder, args.Seq);

        return Wrap(builder, SystemPacket.Ping, pingOffset.Value);
    }
}

internal class ReqUserInfoPacketBuilder: PacketBABuilder<ReqUserInfoArgs>
{
    public override byte[] Build(ReqUserInfoArgs args)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        ReqUserInfo.StartReqUserInfo(builder);
        Offset<ReqUserInfo> offsetUserInfo = ReqUserInfo.EndReqUserInfo(builder); 

        return Wrap(builder, SystemPacket.ReqUserInfo, offsetUserInfo.Value);
    }
}

internal class ResUserInfoPacketBuilder: PacketBABuilder<ResUserInfoArgs>
{
    public override byte[] Build(ResUserInfoArgs args)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        ResUserInfo.StartResUserInfo(builder);

        Offset<GuidType> playerId = GuidType.CreateGuidType(builder, args.playerId.ToByteArray());
        ResUserInfo.AddPlayerId(builder, playerId);
        StringOffset playerName = builder.CreateString(args.playerName);
        ResUserInfo.AddPlayerName(builder, playerName);
        ResUserInfo.AddLevel(builder, args.level);
        ResUserInfo.AddExp(builder, args.exp);
        Offset<ResUserInfo> offsetUserInfo = ResUserInfo.EndResUserInfo(builder);

        return Wrap(builder, SystemPacket.ResUserInfo, offsetUserInfo.Value);
    }
}
internal class ReqCreateNewUserPacketBuilder: PacketBABuilder<ReqCreateNewUserArgs>
{
    public override byte[] Build(ReqCreateNewUserArgs args)
    {
        var builder = CreateBuilder();
        StringOffset playerName = builder.CreateString(args.PlayerName);
        Offset<ReqCreateNewUser> offsetCreateNewUser = ReqCreateNewUser.CreateReqCreateNewUser(builder, playerName);
        return Wrap(builder, SystemPacket.ReqCreateNewUser, offsetCreateNewUser.Value);
    }
}
internal class ResCreateNewUserPacketBuilder : PacketBABuilder<ResCreateNewUserArgs>
{
    public override byte[] Build(ResCreateNewUserArgs args)
    {
        var builder = CreateBuilder();
        Offset<ResCreateNewUser> offsetCreateNewUser = ResCreateNewUser.CreateResCreateNewUser(builder, args.ErrorCode);
        return Wrap(builder, SystemPacket.ResCreateNewUser, offsetCreateNewUser.Value);
    }
}
