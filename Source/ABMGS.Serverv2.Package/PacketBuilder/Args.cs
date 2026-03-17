using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;


// Player.fbs
public record PingArgs(int Seq) : IPacketBuildArgs;
public record PongArgs(int Seq) : IPacketBuildArgs;
public record ReqUserInfoArgs(): IPacketBuildArgs;
public record ResUserInfoArgs(Guid PlayerId, string PlayerName): IPacketBuildArgs;
public record ReqUpdatePlayerNameArgs(string PlayerName) : IPacketBuildArgs;
public record ResUpdatePlayerNameArgs(int Result, string Message): IPacketBuildArgs;

public record ReqDirectDeliveryDataArgs(Guid ToPlayerId, string Message, DirectDeliveryDataType DateType) : IPacketBuildArgs;
public record ResDirectDeliveryDataArgs(PacketErrorCodes ErrorCode, string Message) : IPacketBuildArgs;
public record OnDirectDeliveryDataArgs(Guid FromPlayerId, string Message, DirectDeliveryDataType DataType) : IPacketBuildArgs;

public record ReqCreateRoomArgs(string name, bool isPrivate = true, string password = "", int maxCount = 1) : IPacketBuildArgs;
public record ResCreateRoomArgs(int result, string message, Guid roomId) : IPacketBuildArgs;

public record ReqJoinRoomArgs(Guid roomId, string password) : IPacketBuildArgs;
public record ResJoinRoomArgs(int result, string message): IPacketBuildArgs;

public record ReqLeaveRoomArgs(Guid roomId) : IPacketBuildArgs;
public record ResLeaveRoomArgs(int result, string message) : IPacketBuildArgs;

public record ReqBroadcastRoomArgs(Guid roomId, Guid from, string message) : IPacketBuildArgs;
public record ResBroadcastRoomArgs(int result, string message) : IPacketBuildArgs;
public record BroadcastRoomArgs(Guid from, string message) : IPacketBuildArgs;



