using SyncnetPlatform.Interfaces.Network.Utils;

namespace SyncnetPlatform.Network.Utils;

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
