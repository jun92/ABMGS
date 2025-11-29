using Microsoft.EntityFrameworkCore;
using SyncnetPlatform.Databases;
using System.ComponentModel.DataAnnotations.Schema;


public interface IMakeExtendEntity
{

}

public class MakeExtendEntity : IMakeExtendEntity
{
    public void Configure()
    {

    }
}

public class SyncnetDbContextExtend : SyncnetDbContext
{

    public DbSet<PlayerDataModelExtend> playerExtend;
    public SyncnetDbContextExtend(DbContextOptions<SyncnetDbContextExtend> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerDataModelExtend>().HasOne<PlayerDataModel>(p => p.PlayerDataModel);
        //modelBuilder.Entity<PlayerDataModel>().HasOne<PlayerDataModelExtend>(p => p.);
        base.OnModelCreating(modelBuilder);
    }


}

[Table("players_extend")]
public class PlayerDataModelExtend
{
    public int Id { get; set; }
    public int Level { get; set; }
    public ulong Exp { get; set; }
    public PlayerDataModel PlayerDataModel { get; set; }
}

