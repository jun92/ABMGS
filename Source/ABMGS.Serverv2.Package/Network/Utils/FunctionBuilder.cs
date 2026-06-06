using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Network.Attributes;
using Google.FlatBuffers;
using System.Linq.Expressions;
using System.Reflection;

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

    public static Func<object, Task> BuildFunctionWithParameterType<HoldingClassType>(
        HoldingClassType classInstance, 
        MethodInfo method) 
    {
        PacketHandlerAttribute? packetHandlerAttribute = method.GetCustomAttribute<PacketHandlerAttribute>();
        ArgumentNullException.ThrowIfNull(packetHandlerAttribute);
        ArgumentNullException.ThrowIfNull(classInstance);

        Expression packetHandlerInstanceExpression = Expression.Constant(classInstance, classInstance.GetType());
        ParameterExpression parameter = Expression.Parameter(typeof(object));
        var convertedParamExpression = Expression.Convert(parameter, packetHandlerAttribute.PacketType);

        MethodCallExpression methodCallExpression = Expression.Call(
            packetHandlerInstanceExpression,
            method,
            convertedParamExpression
            );

        var lambda = Expression.Lambda<Func<object, Task>>(methodCallExpression, parameter);

        return lambda.Compile();
    }
}
