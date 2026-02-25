using Google.FlatBuffers;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SyncnetPlatform.Extensions;

public static class FGuidExtension
{
    public static Offset<GuidType> ToGuidType(this Guid guid, FlatBufferBuilder builder)
    {
        Span<byte> guidBytes = stackalloc byte[16];
        MemoryMarshal.TryWrite(guidBytes, guid);

        ulong low = BitConverter.ToUInt64(guidBytes.Slice(0, 8));
        ulong high = BitConverter.ToUInt64(guidBytes.Slice(8, 16));

        return GuidType.CreateGuidType(builder, low, high);

    }
}
