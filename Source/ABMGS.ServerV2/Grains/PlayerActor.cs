using ABMGS.ServerV2.Enums;
using Google.FlatBuffers;
using Microsoft.AspNetCore.Routing.Template;
using SyncnetPlatform.Dto;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ABMGS.ServerV2.Grains;
public class NetworkBuffer : IDisposable
{
    private readonly Pipe _pipe;
    private readonly int _bufferSize;

    public NetworkBuffer(int bufferSize)
    {
        _pipe = new Pipe();
        _bufferSize = bufferSize;
    }

    public Memory<byte> GetReceiveBuffer() => _pipe.Writer.GetMemory(_bufferSize);
    public void AddBuffer(int receivedByteCount) => _pipe.Writer.Advance(receivedByteCount);
    public async Task FinishReceived()
    {
        FlushResult result = await _pipe.Writer.FlushAsync();
        await _pipe.Writer.CompleteAsync();
    }

    public async Task<byte[]> Read()
    {
        ReadResult readResult = await _pipe.Reader.ReadAsync();
        return readResult.Buffer.ToArray<byte>();

    }
    public void Dispose()
    {
        _pipe.Reset();
    }
}


public interface IFuncWrapper
{
    public void Invoke(object data);
    Type ParameterType { get; }
}

public class FuncWrapper<T> : IFuncWrapper
{
    private readonly Action<T> _action;
    public FuncWrapper(Action<T> action)
    {
        _action = action;
    }
    public Type ParameterType => typeof(T);

    public void Invoke(object data) => _action((T)data);
}
public class FlatBufferParser
{
    public IDictionary<SystemPacket, IFuncWrapper> _callTable = new Dictionary<SystemPacket, IFuncWrapper>();

    public FlatBufferParser()
    {


        
    }

    
    public void Deserialize(byte[] data)
    {
    
    }

    public void HandleLoginRequest(LoginRequest loginRequest)
    {
        // Handle LoginRequest
    }
    public void HandleMoveRequest(MoveRequest moveRequest)
    {
        // Handle MoveRequest
    }   

}

public class MessageDelivery
{

}


public interface IGameSessionActor : IGrainWithGuidKey
{
    public Task StartGameLoop(string uniquePlayerId, WebSocket webSocket, CancellationToken cancellationToken);
}

public class GameSessionActor : Grain, IGameSessionActor 
{
    private readonly ILogger<GameSessionActor> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly FlatBufferPacketRouter _routeTable;
    private readonly ICustomPacketHandler _customPacketHandler;

    public GameSessionActor(
        ILogger<GameSessionActor> logger, 
        IClusterClient clusterClient, 
        ICustomPacketHandler customPacketHandler,
        FlatBufferPacketRouter routeTable)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _routeTable = routeTable;
        _customPacketHandler = customPacketHandler;

        
    }

    public void Initialize()
    {
        PacketWrapper packet = PacketWrapper.GetRootAsPacketWrapper(BuildDummyPacket());
        _routeTable.BuildParamExtractionFuncs(packet);
        _routeTable.BuildPacketHandlerFunctions<ICustomPacketHandler>(_customPacketHandler);
    }
    private ByteBuffer BuildDummyPacket()
    {
        FlatBufferBuilder flatBufferBuilder = new FlatBufferBuilder(1024);
        Offset<Dummy> dummy = Dummy.CreateDummy(flatBufferBuilder, 0);
        flatBufferBuilder.Finish(dummy.Value);
        return flatBufferBuilder.DataBuffer;
    }

    public async Task StartGameLoop(string uniquePlayerId, WebSocket SocketObject, CancellationToken abnormalExitToken)
    {

        ArgumentNullException.ThrowIfNullOrEmpty(uniquePlayerId);
        ArgumentNullException.ThrowIfNull(SocketObject);

        IPlayerActor playerActor = _clusterClient.GetGrain<IPlayerActor>(new Guid(uniquePlayerId));
        FlatBufferParser parser = new FlatBufferParser();

        bool IsGameLoopValid = true;

        //Loop to receive data from the WebSocket
        while (IsGameLoopValid && !abnormalExitToken.IsCancellationRequested)
        {
            
            using (NetworkBuffer NBuf = new(4096))
            {
                while (true)
                {
                    ValueWebSocketReceiveResult result = await SocketObject.ReceiveAsync(NBuf.GetReceiveBuffer(), abnormalExitToken);
                    NBuf.AddBuffer(result.Count);

                    if (result.EndOfMessage == true)
                    {
                        await NBuf.FinishReceived();
                        break;
                    }
                }
                _routeTable.Execute(PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(await NBuf.Read())));
            }
        }
        await Task.CompletedTask;
    }
}

public interface IFlatBufferSerializer : IGrainWithGuidKey
{
}



public interface IPlayerActor : IGrainWithGuidKey
{

}

public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;
    }
}

public interface INetworkReceiveActor : IGrainWithStringKey
{

}
public interface INetworkParserActor : IGrainWithStringKey
{

}

public class NetworkReceiveActor : Grain, INetworkReceiveActor
{
    public void ReceivingLoop(WebSocket webSocket)
    {
    }
}

public class NetworkParserActor : Grain, INetworkParserActor
{

}


public interface ICustomPacketHandler
{

}
public partial class CustomPacketHandler : ICustomPacketHandler
{
    private readonly ILogger<CustomPacketHandler> _logger;
    public CustomPacketHandler(ILogger<CustomPacketHandler> logger)
    {
        _logger = logger;
    }

    [PacketHandler(typeof(LoginRequest))]
    public void HandleLoginRequest(LoginRequest loginRequest)
    {
        _logger.LogInformation($"Id: {loginRequest.Id}, From: {loginRequest.From}, Count: {loginRequest.Count}");
    }
   
}

public partial class CustomPacketHandler
{
    [PacketHandler(typeof(MoveRequest))]
    public void HandleMoveRequest(MoveRequest moveRequest)
    {
        _logger.LogInformation($"Id: {moveRequest.Id}, X: {moveRequest.X}, Y: {moveRequest.Y}");
    }
}


[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class PacketHandlerAttribute : Attribute
{
    public Type PacketType { get; }
    public PacketHandlerAttribute(Type packetType)
    {
        PacketType = packetType;
    }
}


public interface IPacketRouter
{
    public void Execute(object packet);
}

public class JsonPacketRouter : IPacketRouter
{
    private readonly ILogger<JsonPacketRouter> _logger;
    public JsonPacketRouter(ILogger<JsonPacketRouter> logger)
    {
        _logger = logger;
    }
    public void Execute(object packet)
    {
        throw new NotImplementedException();
    }
}
public class FlatBufferPacketRouter : IPacketRouter
{
    private readonly ILogger<FlatBufferPacketRouter> _logger;
    private readonly IDictionary<SystemPacket, Action<object>> _packetHandlerTable = new Dictionary<SystemPacket, Action<object>>();
    private readonly IDictionary<SystemPacket, Func<PacketWrapper, object>> _paramExtractionFuncTable = new Dictionary<SystemPacket, Func<PacketWrapper, object>>();

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
                    _packetHandlerTable[packetType] = FunctionBuilder.BuildFunctionWithParameterType<CustomPackerHandlerType>(handler, method);
                }
            }
        }
    }

    public void Execute(object packet)
    {
        Execute((PacketWrapper)packet);
    }
}

public static class FunctionBuilder
{
    public static Func<HoldingClassType, object> BuildFunctionWithReturnType<HoldingClassType>(
        MethodInfo method) 
        where HoldingClassType : IFlatbufferObject
    {
        ParameterExpression instanceParameter = Expression.Parameter(typeof(HoldingClassType));
        MethodCallExpression callExpression = Expression.Call(instanceParameter, method);
        Expression boxed = Expression.Convert(callExpression, typeof(object));
        Expression<Func<HoldingClassType, object>> lambda = Expression.Lambda<Func<HoldingClassType, object>>(boxed, instanceParameter);
        return lambda.Compile();
    }

    public static Action<object> BuildFunctionWithParameterType<HoldingClassType>(
        HoldingClassType classInstance, 
        MethodInfo method) 
        where HoldingClassType: ICustomPacketHandler
    {
        PacketHandlerAttribute? packetHandlerAttribute = method.GetCustomAttribute<PacketHandlerAttribute>();
        ArgumentNullException.ThrowIfNull(packetHandlerAttribute);

        Expression packetHandlerInstanceExpression = Expression.Constant(classInstance, typeof(HoldingClassType));
        ParameterExpression parameter = Expression.Parameter(typeof(object));
        var convertedParamExpression = Expression.Convert(parameter, packetHandlerAttribute.PacketType);

        MethodCallExpression methodCallExpression = Expression.Call(
            packetHandlerInstanceExpression,
            method,
            convertedParamExpression
            );

        var lambda = Expression.Lambda<Action<object>>(methodCallExpression, parameter);

        return lambda.Compile();
    }
}
