using Google.FlatBuffers;
using Orleans.Concurrency;
using SyncnetPlatform.Extensions;
using TGame.Packets;

namespace SyncnetPlatform.Tests;

public partial class ABMGS_TestMain
{
    private static byte[] BuildTGameReqActionSetReady(Guid playerId, bool readyState)
    {
        FlatBufferBuilder builder = new(512);

        StringOffset playerIdOffset = builder.CreateString(playerId.ToString());
        
        TGameReqActionSetReady.StartTGameReqActionSetReady(builder);
        TGameReqActionSetReady.AddPlayerId(builder, playerIdOffset);
        TGameReqActionSetReady.AddReadyState(builder, readyState);
        Offset<TGameReqActionSetReady> offset = TGameReqActionSetReady.EndTGameReqActionSetReady(builder);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }

    private static byte[] BuildTGameReqActionPutMarker(Guid playerId, int x, int y)
    {
        FlatBufferBuilder builder = new(512);
        StringOffset playerIdOffset = builder.CreateString(playerId.ToString());
        TGameReqActionPutItem.StartTGameReqActionPutItem(builder);
        TGameReqActionPutItem.AddX(builder, x);
        TGameReqActionPutItem.AddY(builder, y);
        TGameReqActionPutItem.AddPlayerId(builder, playerIdOffset);
        builder.Finish(TGameReqActionPutItem.EndTGameReqActionPutItem(builder).Value);
        return builder.SizedByteArray();
    }
    
}
