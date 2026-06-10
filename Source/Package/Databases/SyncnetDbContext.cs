using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace SyncnetPlatform.Databases;

public class SyncnetDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<PlayerData> Players { get; set; }
    public DbSet<PlayerExternalIdentities> ExternalIdentities { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreating_Player(modelBuilder);
        OnModelCreating_ExternalIdentities(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected void OnModelCreating_Player(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerData>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedOnAdd();

            e.HasIndex(p => p.PlayerId).IsUnique();
        });

    }
    protected void OnModelCreating_ExternalIdentities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerExternalIdentities>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedOnAdd();

            e.Property(p => p.IdProvider).HasConversion<string>().HasMaxLength(64);

            e.Property(p => p.IdExternal).IsRequired().HasMaxLength(256);
            e.Property(p => p.Created).HasDefaultValueSql("now() AT TIME ZONE 'utc'");
            
            e.HasIndex(p => new {p.IdProvider, p.IdExternal }).IsUnique();
        });
    }
   
}

public enum IdProviderType
{
    Guest,
    GooglePlay,
    Steam,
    Apple,
    EpicGames
}

[Table("player_data")]
public class PlayerData
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}


[Table("player_external_identities")]
public class PlayerExternalIdentities
{
    public int Id { get; set; }
    public IdProviderType IdProvider { get; set; }
    public string IdExternal { get; set; } = String.Empty;
    public Guid SyncnetId { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;

}

