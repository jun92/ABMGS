using Google.FlatBuffers;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;


// Player.fbs
[GenerateSerializer] public record PingArgs(int Seq) : IPacketBuildArgs;
[GenerateSerializer] public record PongArgs(int Seq) : IPacketBuildArgs;
[GenerateSerializer] public record ReqUserInfoArgs(): IPacketBuildArgs;
[GenerateSerializer] public record ResUserInfoArgs(Guid PlayerId, string PlayerName, byte[] PlayerExtendData): IPacketBuildArgs;
[GenerateSerializer] public record ReqUpdatePlayerNameArgs(string PlayerName) : IPacketBuildArgs;
[GenerateSerializer] public record ResUpdatePlayerNameArgs(PacketErrorCodes Result): IPacketBuildArgs;

[GenerateSerializer] public record ReqUserActionForUpdatePlayerExtendDataArgs(string ActionType, byte[] ActionParameters) : IPacketBuildArgs;
[GenerateSerializer] public record ResUserActionForUpdatePlayerExtendDataArgs(PacketErrorCodes Result, string Message, byte[] updatedPlayerExtendData) : IPacketBuildArgs;

[GenerateSerializer] public record ReqDirectDeliveryDataArgs(Guid ToPlayerId, string Message, DirectDeliveryDataType DateType) : IPacketBuildArgs;
[GenerateSerializer] public record ResDirectDeliveryDataArgs(PacketErrorCodes ErrorCode) : IPacketBuildArgs;
[GenerateSerializer] public record OnDirectDeliveryDataArgs(Guid FromPlayerId, string Message, DirectDeliveryDataType DataType) : IPacketBuildArgs;

[GenerateSerializer] public record ReqCreateRoomArgs(string name, bool isPrivate = true, string password = "", int maxCount = 1, byte[]? PlayerMetadata = null) : IPacketBuildArgs;
[GenerateSerializer] public record ResCreateRoomArgs(PacketErrorCodes result, Guid roomId, byte[]? RoomState = null) : IPacketBuildArgs;

[GenerateSerializer] public record ReqJoinRoomArgs(Guid roomId, string password) : IPacketBuildArgs;
[GenerateSerializer] public record ResJoinRoomArgs(PacketErrorCodes result, int AppErrorCode, byte[]? roomState = null): IPacketBuildArgs;
[GenerateSerializer] public record OnPlayerJoinRoomArgs(Guid roomId, Guid playerId, string playerName, byte[]? PlayerMetadata): IPacketBuildArgs;

[GenerateSerializer] public record ReqPlayerListInRoomArgs(Guid roomId) : IPacketBuildArgs;
[GenerateSerializer] public record ResPlayerListInRoomArgs(Guid roomId, PlayerInfoInRoomArgs[] playerInfo) : IPacketBuildArgs;
[GenerateSerializer] public record PlayerInfoInRoomArgs(Guid playerId, string playerName, byte[] PlayerMetadata);

[GenerateSerializer] public record OnPlayRoomUpdateArgs(Guid RoomId, byte[] PlayRoomMetadata) : IPacketBuildArgs;
[GenerateSerializer] public record OnPlayRoomUpdatePlayerArgs(Guid PlayerId, byte[] PlayerMetadata) : IPacketBuildArgs;

[GenerateSerializer] public record ReqPlayerActionToPlayRoomArgs(Guid RoomId, string ActionType, byte[] ActionParameter) : IPacketBuildArgs;
[GenerateSerializer] public record ResPlayerActionToPlayRoomArgs(PacketErrorCodes result, int app_error_code) : IPacketBuildArgs;
[GenerateSerializer] public record ReqLeaveRoomArgs(Guid roomId) : IPacketBuildArgs;
[GenerateSerializer] public record ResLeaveRoomArgs(PacketErrorCodes result) : IPacketBuildArgs;
[GenerateSerializer] public record OnPlayerLeaveRoomArgs(Guid roomId, Guid playerId, string playerName): IPacketBuildArgs;

[GenerateSerializer] public record ReqBroadcastRoomArgs(Guid roomId, Guid from, string message) : IPacketBuildArgs;
[GenerateSerializer] public record ResBroadcastRoomArgs(PacketErrorCodes result) : IPacketBuildArgs;
[GenerateSerializer] public record BroadcastRoomArgs(Guid from, string message) : IPacketBuildArgs;

[GenerateSerializer] public record DeliverCustomPacketArgs(DeliverDestination Dest, byte[] CustomData) : IPacketBuildArgs;