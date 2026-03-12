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
        ulong high = BitConverter.ToUInt64(guidBytes.Slice(8, 8));

        return GuidType.CreateGuidType(builder, low, high);

    }
    public static void ThrowIfInvalidGuid(this Guid guid)
    {
        if(guid == Guid.Empty)
        {
            throw new ArgumentException("Invalid(empty) Guid", nameof(guid));
        }
    }
}
