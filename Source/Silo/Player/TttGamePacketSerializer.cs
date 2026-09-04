using Google.FlatBuffers;
using TGame.Packets;

namespace Silo.Player;
public class TttGamePacketSerializer
{

    public TGameReqActionSetReady DeserializeGameReqActionSetReady(byte[] parameter)
    {
        return TGameReqActionSetReady.GetRootAsTGameReqActionSetReady(new ByteBuffer(parameter));
    }

    public byte[] SerializeNotiftGameStarted(Guid firstPlayerId)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(128);
        StringOffset firstPlayerIdOffset = builder.CreateString(firstPlayerId.ToString());
        Offset<TGameNotifyGameStarted> offset = TGameNotifyGameStarted.CreateTGameNotifyGameStarted(builder, firstPlayerIdOffset);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }

    public byte[] SerializeNotiftGameEnded(Guid winnerPlayerId)
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(128);
        StringOffset winnerPlayerIdOffset = builder.CreateString(winnerPlayerId.ToString());
        Offset<TGameNotifyGameEnd> offset = TGameNotifyGameEnd.CreateTGameNotifyGameEnd(builder, winnerPlayerIdOffset);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }

}
