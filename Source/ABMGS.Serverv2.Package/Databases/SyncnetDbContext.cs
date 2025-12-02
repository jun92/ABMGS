using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace SyncnetPlatform.Databases;

public class SyncnetDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<PlayerDataModel> players { get; set; }
    public DbSet<IdProviderMappingModel> idProviderMapping { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreating_Player(modelBuilder);
        OnModelCreating_IdProviderMapping(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected void OnModelCreating_Player(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerDataModel>().ToTable("players");
        modelBuilder.Entity<PlayerDataModel>().HasKey(p => p.Id);
        modelBuilder.Entity<PlayerDataModel>().Property(p => p.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<PlayerDataModel>().HasIndex(p => p.PlayerId).IsUnique();

    }
    protected void OnModelCreating_IdProviderMapping(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdProviderMappingModel>().ToTable("id_provider_mapping");
        modelBuilder.Entity<IdProviderMappingModel>().HasKey(p => p.Id);
        modelBuilder.Entity<IdProviderMappingModel>().Property(p => p.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<IdProviderMappingModel>().HasIndex(p => new { p.ProviderId, p.SyncnetPlayerId });
    }
}

public enum IdProviderType
{
    Guest = 100,
    GooglePlay,
    Steam,
    Apple,
    EpicGames
}

public class IdProviderMappingModel
{
    public int Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public int SyncnetPlayerId { get; set; }
    public IdProviderType IdProviderType { get; set; }
}

public class PlayerDataModel
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
}


