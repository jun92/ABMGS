using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Network.Attributes;
using Google.FlatBuffers;
using System.Linq.Expressions;
using System.Reflection;

namespace ABMGS.ServerV2.SyncnetPlatform.Network.Utils;

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
