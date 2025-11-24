using Google.FlatBuffers;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Interfaces.Network.Utils;

public interface IPacketRouter
{
    void BuildPacketHandlerFunctions<PacketHandlerType>(PacketHandlerType handler) where PacketHandlerType : IPacketHandler;
    void BuildParamExtractionFuncs<PacketWrapperType>() where PacketWrapperType : IFlatbufferObject;
    void Execute(object packet, PacketContext ctx);
}
