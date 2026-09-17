using Google.FlatBuffers;
using SyncnetPlatform.Extensions;
using TGame.Packets;

namespace SyncnetPlatform.Tests;

public partial class ABMGS_TestMain
{
    private static byte[] BuildTGameReqActionSetReady(Guid playerId, bool readyState)
    {
        FlatBufferBuilder builder = new(1024);

        StringOffset playerIdOffset = builder.CreateString(playerId.ToString());
        
        TGameReqActionSetReady.StartTGameReqActionSetReady(builder);
        TGameReqActionSetReady.AddPlayerId(builder, playerIdOffset);
        TGameReqActionSetReady.AddReadyState(builder, readyState);
        Offset<TGameReqActionSetReady> offset = TGameReqActionSetReady.EndTGameReqActionSetReady(builder);
        builder.Finish(offset.Value);
        return builder.SizedByteArray();
    }
    
}
