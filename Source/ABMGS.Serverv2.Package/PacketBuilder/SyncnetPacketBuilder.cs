using System.Collections.Concurrent;
using System.Reflection;
namespace SyncnetPlatform.Network.Utils;
public class SyncnetPacketBuilder
{
    private static readonly IDictionary<Type, object> _cache = new ConcurrentDictionary<Type, object>();

    public static byte[] Build<TArgs>(TArgs args) where TArgs : IPacketBuildArgs
    {
        try
        {
            if (!_cache.TryGetValue(args.GetType(), out var builder))
            {
                var type = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(
                        t =>
                            t.GetInterfaces().Any(i =>
                                i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IPacketByteArrayBuilder<>) &&
                                i.GetGenericArguments()[0] == typeof(TArgs)
                            )

                    ) ?? throw new MissingMethodException($"No builder found for {typeof(TArgs).Name}");
                builder = Activator.CreateInstance(type) ?? throw new BadImageFormatException();
                _cache.Add(args.GetType(), builder);
            }
            return ((IPacketByteArrayBuilder<TArgs>)builder).Build(args);
        }
        catch (Exception ex)
        {
            throw new FlatBufferPacketBuildException("FlatBuffer build error", ex);
        }
    }
}

public class FlatBufferPacketBuildException : Exception
{
    public FlatBufferPacketBuildException(string message, Exception ex) :base(message, ex)
    {
    }
}
