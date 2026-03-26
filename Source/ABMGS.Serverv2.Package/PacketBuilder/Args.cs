using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;


// Player.fbs
public record PingArgs(int Seq) : IPacketBuildArgs;
public record PongArgs(int Seq) : IPacketBuildArgs;
public record ReqUserInfoArgs(): IPacketBuildArgs;
public record ResUserInfoArgs(Guid PlayerId, string PlayerName): IPacketBuildArgs;
public record ReqUpdatePlayerNameArgs(string PlayerName) : IPacketBuildArgs;
public record ResUpdatePlayerNameArgs(PacketErrorCodes Result): IPacketBuildArgs;

public record ReqDirectDeliveryDataArgs(Guid ToPlayerId, string Message, DirectDeliveryDataType DateType) : IPacketBuildArgs;
public record ResDirectDeliveryDataArgs(PacketErrorCodes ErrorCode) : IPacketBuildArgs;
public record OnDirectDeliveryDataArgs(Guid FromPlayerId, string Message, DirectDeliveryDataType DataType) : IPacketBuildArgs;

public record ReqCreateRoomArgs(string name, bool isPrivate = true, string password = "", int maxCount = 1) : IPacketBuildArgs;
public record ResCreateRoomArgs(PacketErrorCodes result, Guid roomId) : IPacketBuildArgs;

public record ReqJoinRoomArgs(Guid roomId, string password) : IPacketBuildArgs;
public record ResJoinRoomArgs(PacketErrorCodes result): IPacketBuildArgs;
public record OnPlayerJoinRoomArgs(Guid roomId, Guid playerId, string playerName): IPacketBuildArgs;

public record ReqLeaveRoomArgs(Guid roomId) : IPacketBuildArgs;
public record ResLeaveRoomArgs(PacketErrorCodes result) : IPacketBuildArgs;

public record ReqBroadcastRoomArgs(Guid roomId, Guid from, string message) : IPacketBuildArgs;
public record ResBroadcastRoomArgs(PacketErrorCodes result) : IPacketBuildArgs;
public record BroadcastRoomArgs(Guid from, string message) : IPacketBuildArgs;



