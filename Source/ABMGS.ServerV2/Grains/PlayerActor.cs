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

    public GameSessionActor(ILogger<GameSessionActor> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task StartGameLoop(string uniquePlayerId, WebSocket SocketObject, CancellationToken abnormalExitToken)
    {
        IPlayerActor playerActor = _clusterClient.GetGrain<IPlayerActor>(new Guid(uniquePlayerId));
        FlatBufferParser parser = new FlatBufferParser();

        bool IsGameLoopValid = true;

        using (NetworkBuffer NBuf = new(4096))
        {
            //Loop to receive data from the WebSocket
            while (IsGameLoopValid && !abnormalExitToken.IsCancellationRequested)
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
            }
            // Get the received data
            byte[] receivedData = await NBuf.Read();
            parser.Deserialize(receivedData);
            await Task.CompletedTask;
        }
    }
}

public interface IFlatBufferSerializer : IGrainWithGuidKey
{
}



public interface IPlayerActor : IGrainWithGuidKey
{
    public Task StartGameLoop(WebSocket SocketHandle, string UniquePlayerId, CancellationToken AbnormalExitToken);
    // public Task<INetworkReceiveActor> GetNetworkReceiveActor();
}

public class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;

    public PlayerActor(ILogger<PlayerActor> logger)
    {
        _logger = logger;
    }
    public async Task StartGameLoop(WebSocket SocketHandle, string UniquePlayerId, CancellationToken AbnormalExitToken)
    {
        #region Validations
        ArgumentNullException.ThrowIfNullOrEmpty(UniquePlayerId);
        ArgumentNullException.ThrowIfNull(SocketHandle);
        #endregion

        bool IsGameLoopValid = true;    

        using (NetworkBuffer NBuf = new NetworkBuffer(4096))
        {
            //Loop to receive data from the WebSocket
            while (IsGameLoopValid && !AbnormalExitToken.IsCancellationRequested)
            {
                while (true)
                {
                    ValueWebSocketReceiveResult result = await SocketHandle.ReceiveAsync(NBuf.GetReceiveBuffer(), AbnormalExitToken);
                    NBuf.AddBuffer(result.Count);

                    if (result.EndOfMessage == true)
                    {
                        await NBuf.FinishReceived();
                        break;
                    }
                }
            }

            // Get the received data
            byte[] receivedData = await NBuf.Read();
            
            PacketWrapper packetWrapper = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(receivedData));
            switch(packetWrapper.SystemPacketType)
            {
                case SystemPacket.LoginRequest:
                    var loginRequest = packetWrapper.SystemPacketAsLoginRequest();
                    _logger.LogInformation("Received LoginRequest with Username: {Username}", loginRequest.Id);
                    break;
                case SystemPacket.MoveRequest:
                    MoveRequest moveRequest = packetWrapper.SystemPacketAsMoveRequest();
                    _logger.LogInformation("Received MoveRequest with Direction: {Direction}", moveRequest.X);
                    break;
                default:
                    _logger.LogWarning("Received unknown packet type: {PacketType}", packetWrapper.SystemPacketType);
                    break;
            }


        }
    }
    public Task<INetworkReceiveActor> GetNetworkReceiveActor()
    {
        string NetworkReceiveActorId = string.Join("/", this.GetGrainId().GetGuidKey().ToString(), ActorSuffixNames.NetworkReceiveActor.ToString());
        return Task.FromResult(GrainFactory.GetGrain<INetworkReceiveActor>(NetworkReceiveActorId));
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
        webSocket.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);

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



public class PacketPathBuilder
{
    private readonly IDictionary<SystemPacket, Action<object>> _packetHandlerTable = new Dictionary<SystemPacket, Action<object>>();
    private readonly IDictionary<SystemPacket, Func<object>> _paramExtractionFuncTable = new Dictionary<SystemPacket, Func<object>>();


    public IDictionary<SystemPacket, Action<object>> PacketHandleTable
    {
        get
        {
            return _packetHandlerTable;
        }
    }

    public PacketPathBuilder()
    {
    }

    public void BuildParamExtractionFuncs(PacketWrapper packetWrapper) 
    {
        Type baseClass = typeof(PacketWrapper);

        MethodInfo[] methods = baseClass.GetMethods();

        foreach(MethodInfo method in methods)
        {
            if(method.Name.StartsWith(PacketSuffix.SystemPacket.ToString()))
            {
                if(Enum.TryParse(method.ReturnType.Name, out SystemPacket packetType))
                {
                    _paramExtractionFuncTable[packetType] = FunctionBuilder.BuildFunctionWithReturnType<PacketWrapper>(packetWrapper, method);
                }
            }
        }
    }
    public void BuildPacketHandlerFunctions<CustomPackerHandlerType>(CustomPackerHandlerType handler) where CustomPackerHandlerType : ICustomPacketHandler
    {
        Type baseClass = typeof(CustomPackerHandlerType);
        MethodInfo[] methods = baseClass.GetMethods();
        foreach(MethodInfo method in methods)
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
}

public static class FunctionBuilder
{
    public static Func<object> BuildFunctionWithReturnType<HoldingClassType>(
        HoldingClassType classInstance, 
        MethodInfo method) 
        where HoldingClassType : IFlatbufferObject
    {
        Expression instanceExpression = Expression.Convert(Expression.Constant(classInstance), typeof(HoldingClassType));
        MethodCallExpression callExpression = Expression.Call(instanceExpression, method);
        Type funcType = typeof(Func<>).MakeGenericType(method.ReturnType);
        Expression boxed = Expression.Convert(callExpression, typeof(object));
        Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(boxed, null);

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
