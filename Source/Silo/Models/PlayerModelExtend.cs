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
public class MyGamePlayerDataExtendCreater : IPlayerDataExtendCreater
{
    public void CreateExtendColumns(EntityTypeBuilder<PlayerData> e)
    {
        //e.IndexerProperty<int>(PlayerDataColumn.Level).HasDefaultValue(1);
        //e.IndexerProperty<string>(PlayerDataColumn.Title).HasDefaultValue("No Title");
        //e.IndexerProperty<float>(PlayerDataColumn.PosX).HasDefaultValue(10.0f);
        //e.IndexerProperty<float>(PlayerDataColumn.PosY).HasDefaultValue(-10.0f);
        //e.IndexerProperty<long>(PlayerDataColumn.Exp).HasDefaultValue(0);
        e.IndexerProperty<int>(PlayerDataColumn.CustomLevel).HasDefaultValue(1);
        e.IndexerProperty<long>(PlayerDataColumn.CustomExp).HasDefaultValue(0);
    }
}
