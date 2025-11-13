namespace SyncnetPlatform.Interfaces.Network.Utils;

public interface IFuncWrapper
{
    public void Invoke(object data);
    Type ParameterType { get; }
}
