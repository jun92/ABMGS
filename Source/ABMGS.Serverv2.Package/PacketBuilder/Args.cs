using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;


// Player.fbs
[GenerateSerializer] public record PingArgs(int Seq) : IPacketBuildArgs;
[GenerateSerializer] public record PongArgs(int Seq) : IPacketBuildArgs;
[GenerateSerializer] public record ReqUserInfoArgs(): IPacketBuildArgs;
[GenerateSerializer] public record ResUserInfoArgs(Guid PlayerId, string PlayerName): IPacketBuildArgs;
[GenerateSerializer] public record ReqUpdatePlayerNameArgs(string PlayerName) : IPacketBuildArgs;
[GenerateSerializer] public record ResUpdatePlayerNameArgs(PacketErrorCodes Result): IPacketBuildArgs;

[GenerateSerializer] public record ReqDirectDeliveryDataArgs(Guid ToPlayerId, string Message, DirectDeliveryDataType DateType) : IPacketBuildArgs;
[GenerateSerializer] public record ResDirectDeliveryDataArgs(PacketErrorCodes ErrorCode) : IPacketBuildArgs;
[GenerateSerializer] public record OnDirectDeliveryDataArgs(Guid FromPlayerId, string Message, DirectDeliveryDataType DataType) : IPacketBuildArgs;

[GenerateSerializer] public record ReqCreateRoomArgs(string name, bool isPrivate = true, string password = "", int maxCount = 1) : IPacketBuildArgs;
[GenerateSerializer] public record ResCreateRoomArgs(PacketErrorCodes result, Guid roomId) : IPacketBuildArgs;

[GenerateSerializer] public record ReqJoinRoomArgs(Guid roomId, string password) : IPacketBuildArgs;
[GenerateSerializer] public record ResJoinRoomArgs(PacketErrorCodes result): IPacketBuildArgs;
[GenerateSerializer] public record OnPlayerJoinRoomArgs(Guid roomId, Guid playerId, string playerName): IPacketBuildArgs;

[GenerateSerializer] public record ReqLeaveRoomArgs(Guid roomId) : IPacketBuildArgs;
[GenerateSerializer] public record ResLeaveRoomArgs(PacketErrorCodes result) : IPacketBuildArgs;
[GenerateSerializer] public record OnPlayerLeaveRoomArgs(Guid roomId, Guid playerId): IPacketBuildArgs;

[GenerateSerializer] public record ReqBroadcastRoomArgs(Guid roomId, Guid from, string message) : IPacketBuildArgs;
[GenerateSerializer] public record ResBroadcastRoomArgs(PacketErrorCodes result) : IPacketBuildArgs;
[GenerateSerializer] public record BroadcastRoomArgs(Guid from, string message) : IPacketBuildArgs;



