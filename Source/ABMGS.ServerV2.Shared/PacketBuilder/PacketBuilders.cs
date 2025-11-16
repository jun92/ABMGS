using Google.FlatBuffers;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;

internal class PongPacketBuilder : IPacketByteArrayBuilder<PongArgs>
{
    public byte[] Build(PongArgs args)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        Offset<Pong> pongOffset = Pong.CreatePong(builder, args.Seq+1);
        Offset<PacketWrapper> packetWrapperOffset = PacketWrapper.CreatePacketWrapper(builder, SystemPacket.Pong, pongOffset.Value);
        builder.Finish(packetWrapperOffset.Value);
        
        return builder.SizedByteArray();
    }
}

internal class PingPacketBuilder : IPacketByteArrayBuilder<PingArgs>
{
    public byte[] Build(PingArgs args)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        Offset<Ping> pingOffset = Ping.CreatePing(builder, args.Seq);
        Offset<PacketWrapper> packetWrapperOffset = PacketWrapper.CreatePacketWrapper(
            builder, 
            SystemPacket.Ping, pingOffset.Value);
        builder.Finish(packetWrapperOffset.Value);
        return builder.SizedByteArray();
    }
}

