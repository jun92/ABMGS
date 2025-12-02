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
        modelBuilder.Entity<PlayerDataModelExtend>().HasKey(p => p.Id);
        modelBuilder.Entity<PlayerDataModelExtend>().Property(p => p.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<PlayerDataModelExtend>().HasOne<PlayerDataModel>().WithMany().HasForeignKey(p => p.PlayerId).OnDelete(DeleteBehavior.Cascade);
        base.OnModelCreating(modelBuilder);
    }


}

[Table("players_extend")]
public class PlayerDataModelExtend
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int Level { get; set; }
    public ulong Exp { get; set; }
}

