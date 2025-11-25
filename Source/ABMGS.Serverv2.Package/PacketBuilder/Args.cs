namespace SyncnetPlatform.Network.Utils;

public record PingArgs(int Seq) : IPacketBuildArgs;
public record PongArgs(int Seq) : IPacketBuildArgs;

public record ReqUserInfoArgs(): IPacketBuildArgs;
public record ResUserInfoArgs(Guid playerId, string playerName, int level, ulong exp): IPacketBuildArgs;


