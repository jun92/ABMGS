using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace SyncnetPlatform.Databases;

public class SyncnetDbContext : DbContext
{
    public DbSet<PlayerDataModelExtend>? players;

    public SyncnetDbContext(DbContextOptions<SyncnetDbContextExtend> options): base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreating_Player(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected void OnModelCreating_Player(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerDataModelExtend>().ToTable("players");
        modelBuilder.Entity<PlayerDataModelExtend>().HasKey(p => p.Id);
        modelBuilder.Entity<PlayerDataModelExtend>().Property(p => p.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<PlayerDataModelExtend>().HasIndex(p => p.PlayerId).IsUnique();

    }
}

public class SyncnetDbContextExtend : SyncnetDbContext
{
    public SyncnetDbContextExtend(DbContextOptions<SyncnetDbContextExtend> options) : base(options)
    {

    }
}

public class PlayerDataModel
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}


public partial class PlayerDataModelExtend : PlayerDataModel
{
}
