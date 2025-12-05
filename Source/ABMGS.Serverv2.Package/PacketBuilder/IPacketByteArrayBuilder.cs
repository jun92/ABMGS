namespace SyncnetPlatform.Network.Utils;

internal interface IPacketByteArrayBuilder<TArgs> where TArgs: IPacketBuildArgs
{
    byte[] Build(TArgs args);
}


