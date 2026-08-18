using SyncnetPlatform.Databases;

namespace Silo.Models;

public class TttGamePlayerModelExtend : IPlayerDataExtendCreater
{
    public const string WinCount = "WinCount";
    public const string LoseCount = "LoseCount";
    public const string PlayCount = "PlayCount";
    public IReadOnlyList<(Type, string, object)> RegisterPlayerCustomData()
    {
        return
        [
            (typeof(int), WinCount, 0),
            (typeof(int), LoseCount, 0),
            (typeof(int), PlayCount, 0)
        ];
    }
        
}