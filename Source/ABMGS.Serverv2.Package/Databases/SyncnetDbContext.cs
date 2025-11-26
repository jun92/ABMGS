using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace SyncnetPlatform.Databases;

public class SyncnetDbContext(DbContextOptions<SyncnetDbContext> options) : DbContext(options)
{
    DbSet<Player> players;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreating_Player(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected void OnModelCreating_Player(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().HasKey(p => p.Id);
        modelBuilder.Entity<Player>().Property(p => p.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<Player>().HasIndex(p => p.PlayerId).IsUnique();
    }
}

public class Player
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Level { get; set; }
    public ulong Exp { get; set; }

}
