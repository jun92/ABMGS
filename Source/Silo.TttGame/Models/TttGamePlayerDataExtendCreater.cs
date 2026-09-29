using SyncnetPlatform.Databases;

namespace Silo.TttGame.Models;

public class TttGamePlayerDataExtendDefinition : IPlayerDataExtendDefinition
{
    public const string WinCount = "WinCount";
    public const string LoseCount = "LoseCount";
    public const string PlayCount = "PlayCount";
    public IReadOnlyList<(Type, string, object)> GetExtendDataDefinitions()
    {
        return
        [
            (typeof(int), WinCount, 0),
            (typeof(int), LoseCount, 0),
            (typeof(int), PlayCount, 0)
        ];
    }
        
}