namespace SyncnetPlatform.Actors;

[GenerateSerializer]
public class PlayerState
{
    [Id(0)] public int Id { get; set; }
    [Id(1)] public Guid PlayerId { get; set; }
    [Id(2)] public string PlayerName { get; set; } = String.Empty;

    [Id(3)] public Dictionary<string, object?> Extension { get; set; } = new();

    public object? this[string key]
    {
        get => Extension.TryGetValue(key, out var val) ? val : null;
        set => Extension[key] = value;
    }
}
