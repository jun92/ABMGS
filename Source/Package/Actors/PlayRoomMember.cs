namespace SyncnetPlatform.Actors;

[GenerateSerializer] 
public class PlayRoomMember(Guid roomId, Guid playerId, string playerName, byte[]? playerExtendData)
{
    [Id(0)]
    public Guid RoomId { get; set; } = roomId;

    [Id(1)]
    public Guid PlayerId { get; set; } = playerId;

    [Id(2)]
    public string PlayerName { get; set; } = playerName;

    // One time use only.
    [Id(3)]
    public byte[]? PlayerExtendData { get; set; } = playerExtendData;
}
