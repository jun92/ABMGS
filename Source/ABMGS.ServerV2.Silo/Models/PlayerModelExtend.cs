using Microsoft.EntityFrameworkCore;
using SyncnetPlatform.Databases;
using System.ComponentModel.DataAnnotations.Schema;


public class SyncnetDbContextExtend : SyncnetDbContext
{

    public DbSet<PlayerDataModelExtend> playerExtend { get; set; }
    public SyncnetDbContextExtend(DbContextOptions<SyncnetDbContextExtend> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<PlayerDataModel>().HasOne<PlayerDataModelExtend>(p => p.);
        modelBuilder.Entity<PlayerDataModelExtend>().HasKey(p => p.Id);
        modelBuilder.Entity<PlayerDataModelExtend>().Property(p => p.Id).ValueGeneratedOnAdd();
        base.OnModelCreating(modelBuilder);
    }


}

[Table("players_extend")]
public class PlayerDataModelExtend
{
    public int Id { get; set; }
    public int Level { get; set; }
    public ulong Exp { get; set; }
}

