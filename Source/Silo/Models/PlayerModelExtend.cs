using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Databases;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;


public static class PlayerDataColumn
{
    public const string CustomLevel = "CustomLevel";
    public const string CustomExp = "CustomExp";

}
public class MyGamePlayerDataExtendCreater : IPlayerDataExtendDefinition
{
    public IReadOnlyList<(Type, string, object)> GetExtendDataDefinitions()
    {
        return new List<(Type, string, object)>
        {
            (typeof(int),PlayerDataColumn.CustomLevel, 1 ),
            (typeof(long), PlayerDataColumn.CustomExp, 33)
        };
    }
}
 