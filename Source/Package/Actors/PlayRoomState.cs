namespace SyncnetPlatform.Actors;

public class PlayRoomState
{
    [Id(0)]
    public string DisplayName { get; set; } = String.Empty;
    [Id(1)]
    public string PasswordForEntrance { get; set; } = String.Empty;
    [Id(2)]
    public IPlayRoomCustomState? PlayRoomCustomState { get; set; } = null;
}
