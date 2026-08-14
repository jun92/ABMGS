using SyncnetPlatform.Databases;

namespace Silo.Models;

public class TGamePlayerModelExtend : IPlayerDataExtendCreater
{
    public IReadOnlyList<(Type, string, object)> RegisterPlayerCustomData()
    {
        return
        [
            (typeof(int), "WinCount", 0),
            (typeof(int), "LoseCount", 0),
            (typeof(int), "PlayCount", 0)
        ];
    }

}