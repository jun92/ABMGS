namespace SyncnetPlatform.Network.Utils;

public record PingArgs(int Seq) : IPacketBuildArgs;
public record PongArgs(int Seq) : IPacketBuildArgs;
public record ReqUserInfoArgs(): IPacketBuildArgs;
public record ResUserInfoArgs(Guid playerId, string playerName): IPacketBuildArgs;
public record ReqUpdatePlayerNameArgs(string playerName) : IPacketBuildArgs;
public record ResUpdatePlayerNameArgs(int result, string message): IPacketBuildArgs;


