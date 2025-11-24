using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Attributes;
using Google.FlatBuffers;
using System.Linq.Expressions;
using System.Reflection;
using SyncnetPlatform.Network.Handlers;

namespace SyncnetPlatform.Network.Utils;

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

    public static Action<object, PacketContext> BuildFunctionWithParameterType<HoldingClassType>(
        HoldingClassType classInstance, 
        MethodInfo method) 
    {
        PacketHandlerAttribute? packetHandlerAttribute = method.GetCustomAttribute<PacketHandlerAttribute>();
        ArgumentNullException.ThrowIfNull(packetHandlerAttribute);

        Expression packetHandlerInstanceExpression = Expression.Constant(classInstance, typeof(HoldingClassType));
        ParameterExpression parameter = Expression.Parameter(typeof(object));
        ParameterExpression paramPacketContext = Expression.Parameter(typeof(PacketContext));
        var convertedParamExpression = Expression.Convert(parameter, packetHandlerAttribute.PacketType);

        MethodCallExpression methodCallExpression = Expression.Call(
            packetHandlerInstanceExpression,
            method,
            convertedParamExpression,
            paramPacketContext
            );

        var lambda = Expression.Lambda<Action<object, PacketContext>>(methodCallExpression, parameter, paramPacketContext);

        return lambda.Compile();
    }
}
