using Google.FlatBuffers;
using Silo.Models;
using TGame.Packets;

namespace Silo.Player;
public class TttGamePacketSerializer
{

    public TGameReqActionSetReady DeserializeGameReqActionSetReady(byte[] parameter)
    {
        return TGameReqActionSetReady.GetRootAsTGameReqActionSetReady(new ByteBuffer(parameter));
    }

    public Dictionary<string, object?> DeserializePlayerCustomData(byte[] playerExtendDataArray)
    {
        // FlatBuffer parsing, use your favorite serialize library. ex) protoBuf, json, etc.
        TGamePlayerCustomData playerExtendData = 
            TGamePlayerCustomData.GetRootAsTGamePlayerCustomData(new ByteBuffer(playerExtendDataArray));

        return new Dictionary<string, object?>
        {
            { TttGamePlayerModelExtend.WinCount, playerExtendData.WinCount },
            { TttGamePlayerModelExtend.LoseCount, playerExtendData.LoseCount },
            { TttGamePlayerModelExtend.PlayCount, playerExtendData.PlayCount }
        };
    }

    public byte[] SerializeNotiftGameStarted(Guid firstPlayerId)
    {
        FlatBufferBuilder builder = new(128);
        StringOffset firstPlayerIdOffset = builder.CreateString(firstPlayerId.ToString());
        Offset<TGameNotifyGameStarted> offset = TGameNotifyGameStarted.CreateTGameNotifyGameStarted(builder, firstPlayerIdOffset);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }

    public byte[] SerializeNotiftGameEnded(Guid winnerPlayerId)
    {
        FlatBufferBuilder builder = new(128);
        StringOffset winnerPlayerIdOffset = builder.CreateString(winnerPlayerId.ToString());
        Offset<TGameNotifyGameEnd> offset = TGameNotifyGameEnd.CreateTGameNotifyGameEnd(builder, winnerPlayerIdOffset);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }

}
