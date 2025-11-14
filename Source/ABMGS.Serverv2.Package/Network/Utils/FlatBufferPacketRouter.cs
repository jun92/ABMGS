using Microsoft.Extensions.Logging;
using SyncnetPlatform.Enums;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Protocols.Generated;

using System.Reflection;

namespace SyncnetPlatform.Network.Utils;

public class FlatBufferPacketRouter : IPacketRouter
{
    private readonly ILogger<FlatBufferPacketRouter> _logger;
    private readonly Dictionary<SystemPacket, Action<object>> _packetHandlerTable = [];
    private readonly Dictionary<SystemPacket, Func<PacketWrapper, object>> _paramExtractionFuncTable = [];

    public void Execute(PacketWrapper packetWrapper)
    {
        _packetHandlerTable[packetWrapper.SystemPacketType](_paramExtractionFuncTable[packetWrapper.SystemPacketType](packetWrapper));
    }
    public FlatBufferPacketRouter(ILogger<FlatBufferPacketRouter> logger)
    {
        _logger = logger;
    }

    public void BuildParamExtractionFuncs(PacketWrapper packetWrapper) 
    {
        foreach (MethodInfo method in typeof(PacketWrapper).GetMethods())
        {
            if(method.Name.StartsWith(PacketSuffix.SystemPacket.ToString()))
            {
                if(Enum.TryParse(method.ReturnType.Name, out SystemPacket packetType))
                {
                    _paramExtractionFuncTable[packetType] = FunctionBuilder.BuildFunctionWithReturnType<PacketWrapper>(method);
                    _logger.LogInformation($"Found and stored the function for getting type of {packetType.ToString()}");
                }
            }
        }
    }
    public void BuildPacketHandlerFunctions<CustomPackerHandlerType>(CustomPackerHandlerType handler) where CustomPackerHandlerType : ICustomPacketHandler
    {
        foreach (MethodInfo method in typeof(CustomPackerHandlerType).GetMethods())
        {
            PacketHandlerAttribute? attr = method.GetCustomAttribute<PacketHandlerAttribute>();
            if (attr != null)
            {
                if(Enum.TryParse(attr.PacketType.Name, out SystemPacket packetType))
                {
                    _packetHandlerTable[packetType] = FunctionBuilder.BuildFunctionWithParameterType(handler, method);
                    _logger.LogInformation($"Now binded the type of {packetType.ToString()} to the function: {method.Name} ");
                }
            }
        }
    }

    public void Execute(object packet)
    {
        Execute((PacketWrapper)packet);
    }
}
