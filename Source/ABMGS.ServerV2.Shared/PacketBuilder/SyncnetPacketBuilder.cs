using System.Reflection;
namespace SyncnetPlatform.Network.Utils;
public class SyncnetPacketBuilder
{
    private static readonly Dictionary<Type, object> _cache = new();

    public static byte[] Build<TArgs>(TArgs args) where TArgs : IPacketBuildArgs
    {
        if(!_cache.TryGetValue(args.GetType(), out var builder))
        {
            builder = Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t =>
                    t.GetInterfaces().Any(i =>
                        i.IsGenericType 
                        && i.GetGenericTypeDefinition() == typeof(IPacketByteArrayBuilder<>) 
                        && i.GetGenericArguments()[0] == typeof(TArgs)
                    )
                ) is { } type ? Activator.CreateInstance(args.GetType()) : throw new InvalidOperationException($"No builder found for {typeof(TArgs).Name}");
            
            _cache[typeof(TArgs)] = builder;
        }
        return ((IPacketByteArrayBuilder<TArgs>)builder).Build(args);
    }
}


