using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Enums;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Attributes;
using SyncnetPlatform.Protocols.Generated;
using SyncnetPlatform.Utils.Telemetry;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace SyncnetPlatform.Network.Utils;

public class FlatBufferPacketRouter : IPacketRouter
{
    private record PacketHandlerInfo(Func<object, Task> HandlerFunc, string MethodName);
    private readonly ILogger<FlatBufferPacketRouter> _logger;
    private readonly SyncnetMetricsService _metricsService;
    //private readonly Dictionary<SystemPacket, Func<object, Task>> _packetHandlerTable = [];
    private readonly Dictionary<SystemPacket, PacketHandlerInfo> _packetHandlerTable = [];
    private readonly Dictionary<SystemPacket, Func<PacketWrapper, object>> _paramExtractionFuncTable = [];

    public FlatBufferPacketRouter(ILogger<FlatBufferPacketRouter> logger, SyncnetMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }
    public async Task Execute(PacketWrapper packetWrapper)
    {
        if(_paramExtractionFuncTable.TryGetValue(packetWrapper.SystemPacketType, out var paramGetfunc))
        {
            if(_packetHandlerTable.TryGetValue(packetWrapper.SystemPacketType, out var handlerInfo))
            {
                using var methodActivity = SyncnetTelemetry.Trace.StartActivity(handlerInfo.MethodName, ActivityKind.Internal);
                methodActivity?.SetTag("packet.type", packetWrapper.SystemPacketType.ToString());

                long startTime = Stopwatch.GetTimestamp();
                try
                {
                    await handlerInfo.HandlerFunc(paramGetfunc(packetWrapper));
                    _metricsService.RecordPacketProcessed(packetWrapper.SystemPacketType.ToString(), Stopwatch.GetElapsedTime(startTime).TotalMilliseconds, "Success");
                }
                catch (Exception)
                {
                    _metricsService.RecordPacketProcessed(packetWrapper.SystemPacketType.ToString(), Stopwatch.GetElapsedTime(startTime).TotalMilliseconds, "Error");
                    throw;
                }
                return;
            }
        }
        _logger.LogError($"Not found the handler function for type of {packetWrapper.SystemPacketType.ToString()}");
    }
    public Task Execute(object packet)
    {
        return Execute((PacketWrapper)packet);
    }

    //public void BuildParamExtractionFuncs(PacketWrapper packetWrapper) 
    //{
    //    foreach (MethodInfo method in typeof(PacketWrapper).GetMethods())
    //    {
    //        if(method.Name.StartsWith(PacketSuffix.SystemPacket.ToString()))
    //        {
    //            if(Enum.TryParse(method.ReturnType.Name, out SystemPacket packetType))
    //            {
    //                _paramExtractionFuncTable[packetType] = FunctionBuilder.BuildFunctionWithReturnType<PacketWrapper>(method);
    //                _logger.LogTrace($"Found and stored the function for getting type of {packetType.ToString()}");
    //            }
    //        }
    //    }
    //}
    public void BuildParamExtractionFuncs<PacketWrapperType>() where PacketWrapperType : IFlatbufferObject
    {
        foreach (MethodInfo method in typeof(PacketWrapper).GetMethods())
        {
            if (method.Name.StartsWith(PacketSuffix.SystemPacket.ToString()))
            {
                if (Enum.TryParse(method.ReturnType.Name, out SystemPacket packetType))
                {
                    _paramExtractionFuncTable[packetType] = FunctionBuilder.BuildFunctionWithReturnType<PacketWrapper>(method);
                    _logger.LogTrace($"Found and stored the function for getting type of {packetType.ToString()}");
                }
            }
        }
    }
    public void BuildPacketHandlerFunctions<PacketHandlerType>(PacketHandlerType handler) where PacketHandlerType : IPacketHandler
    {
        foreach (MethodInfo method in handler.GetType().GetMethods())
        {
            _logger.LogInformation("Processing method:" + method.Name);
            PacketHandlerAttribute? attr = method.GetCustomAttribute<PacketHandlerAttribute>();
            if (attr != null)
            {
                if(Enum.TryParse(attr.PacketType.Name, out SystemPacket packetType))
                {
                    var methodName = $"{handler.GetType().Name}.{method.Name}";
                    //_packetHandlerTable[packetType] = FunctionBuilder.BuildFunctionWithParameterType(handler, method);
                    _packetHandlerTable[packetType] = new PacketHandlerInfo(FunctionBuilder.BuildFunctionWithParameterType(handler, method), methodName);
                    _logger.LogInformation($"Now binded the type of {packetType.ToString()} to the function: {method.Name} ");
                }
            }
        }
       
    }

}
