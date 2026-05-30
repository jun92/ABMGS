using Google.FlatBuffers;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Interfaces.Network.Utils;

public interface IPacketRouter
{
    void BuildPacketHandlerFunctions<PacketHandlerType>(PacketHandlerType handler) where PacketHandlerType : IPacketHandler;
    void BuildParamExtractionFuncs<PacketWrapperType>() where PacketWrapperType : IFlatbufferObject;
    Task Execute(object packet, PacketContext ctx);
}
