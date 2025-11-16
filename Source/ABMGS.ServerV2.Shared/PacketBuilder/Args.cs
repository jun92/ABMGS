namespace SyncnetPlatform.Network.Utils;

public record PingArgs(int Seq) : IPacketBuildArgs;
public record PongArgs(int Seq) : IPacketBuildArgs;


