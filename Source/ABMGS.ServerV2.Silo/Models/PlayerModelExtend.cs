using Microsoft.EntityFrameworkCore;
using SyncnetPlatform.Actors;
using SyncnetPlatform.Databases;
using System.ComponentModel.DataAnnotations.Schema;


public class SyncnetDbContextExtend : SyncnetDbContext
{

    public DbSet<PlayerDataModelExtend> PlayerExtend { get; set; }
    public SyncnetDbContextExtend(DbContextOptions<SyncnetDbContextExtend> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerDataModelExtend>().HasKey(p => p.Id);
        modelBuilder.Entity<PlayerDataModelExtend>().Property(p => p.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<PlayerDataModelExtend>()
            .HasOne<PlayerDataModel>()
            .WithMany()
            .OnDelete(DeleteBehavior.Cascade)
            .HasForeignKey(p => p.PlayerId);

        base.OnModelCreating(modelBuilder);
    }


}

public class PlayerDataBehavior : IPlayerDataBehavior
{
    public async Task OnCreateNewPlayer(PlayerDataContext ctx)
    {
        var db = ctx.Db as SyncnetDbContextExtend;
        if (db != null)
        {
            await db.PlayerExtend.AddAsync(new PlayerDataModelExtend
            {
                PlayerId = ctx.PlayerId,
                Level = 1,
                Exp = 0
            });
        }
        else
        {
            throw new InvalidDataException();
        }
    }
}

// Extend table for RPG game
[Table("players_rpg_extend")]
public class PlayerDataModelExtend
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public int Level { get; set; }
    public ulong Exp { get; set; }
}
