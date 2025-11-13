using Google.FlatBuffers;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;

internal class PongPacketBuilder : IPacketByteArrayBuilder<PongArgs>
{
    public byte[] Build(PongArgs args)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(4096);
        Offset<Pong> pongOffset = Pong.CreatePong(builder, args.Seq+1);
        builder.Finish(pongOffset.Value);
        return builder.SizedByteArray();
    }
}


