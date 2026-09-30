using Google.FlatBuffers;
using Silo.TttGame.Models;
using TGame.Packets;

namespace Silo.Player;
public class TttGamePacketSerializer
{

    public static TGameReqActionSetReady DeserializeGameReqActionSetReady(byte[] parameter)
    {
        return TGameReqActionSetReady.GetRootAsTGameReqActionSetReady(new ByteBuffer(parameter));
    }

    public static Dictionary<string, object?> DeserializePlayerCustomData(byte[] playerExtendDataArray)
    {
        // FlatBuffer parsing, use your favorite serialize library. ex) protoBuf, json, etc.
        TGamePlayerCustomData playerExtendData = 
            TGamePlayerCustomData.GetRootAsTGamePlayerCustomData(new ByteBuffer(playerExtendDataArray));

        return new Dictionary<string, object?>
        {
            { TttGamePlayerDataExtendDefinition.WinCount, playerExtendData.WinCount },
            { TttGamePlayerDataExtendDefinition.LoseCount, playerExtendData.LoseCount },
            { TttGamePlayerDataExtendDefinition.PlayCount, playerExtendData.PlayCount }
        };
    }

    public static byte[] SerializeNotifyGameStarted(Guid firstPlayerId)
    {
        FlatBufferBuilder builder = new(128);
        StringOffset firstPlayerIdOffset = builder.CreateString(firstPlayerId.ToString());
        Offset<TGameNotifyGameStarted> offset = TGameNotifyGameStarted.CreateTGameNotifyGameStarted(builder, firstPlayerIdOffset);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }

    public static byte[] SerializeNotifyGameEnded(Guid winnerPlayerId)
    {
        FlatBufferBuilder builder = new(128);
        StringOffset winnerPlayerIdOffset = builder.CreateString(winnerPlayerId.ToString());
        Offset<TGameNotifyGameEnd> offset = TGameNotifyGameEnd.CreateTGameNotifyGameEnd(builder, winnerPlayerIdOffset);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }

}
