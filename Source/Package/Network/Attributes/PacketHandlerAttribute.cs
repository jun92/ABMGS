using System;

namespace SyncnetPlatform.Network.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class PacketHandlerAttribute : Attribute
{
    public Type PacketType { get; }
    public PacketHandlerAttribute(Type packetType)
    {
        PacketType = packetType;
    }
}
