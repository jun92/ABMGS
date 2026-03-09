using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABMGS.ServerV2.Migrations.Models;

public partial class ClusterInfoContext : DbContext
{
    public ClusterInfoContext()
    {
    }

    public ClusterInfoContext(DbContextOptions<ClusterInfoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Orleansmembershiptable> Orleansmembershiptables { get; set; }

    public virtual DbSet<Orleansmembershipversiontable> Orleansmembershipversiontables { get; set; }

    public virtual DbSet<Orleansquery> Orleansqueries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=cluster_info;Username=postgres;Password=1234;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Orleansmembershiptable>(entity =>
        {
            entity.HasKey(e => new { e.Deploymentid, e.Address, e.Port, e.Generation }).HasName("pk_membershiptable_deploymentid");

            entity.ToTable("orleansmembershiptable");

            entity.Property(e => e.Deploymentid)
                .HasMaxLength(150)
                .HasColumnName("deploymentid");
            entity.Property(e => e.Address)
                .HasMaxLength(45)
                .HasColumnName("address");
            entity.Property(e => e.Port).HasColumnName("port");
            entity.Property(e => e.Generation).HasColumnName("generation");
            entity.Property(e => e.Hostname)
                .HasMaxLength(150)
                .HasColumnName("hostname");
            entity.Property(e => e.Iamalivetime)
                .HasPrecision(3)
                .HasColumnName("iamalivetime");
            entity.Property(e => e.Proxyport).HasColumnName("proxyport");
            entity.Property(e => e.Siloname)
                .HasMaxLength(150)
                .HasColumnName("siloname");
            entity.Property(e => e.Starttime)
                .HasPrecision(3)
                .HasColumnName("starttime");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Suspecttimes)
                .HasMaxLength(8000)
                .HasColumnName("suspecttimes");

            entity.HasOne(d => d.Deployment).WithMany(p => p.Orleansmembershiptables)
                .HasForeignKey(d => d.Deploymentid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_membershiptable_membershipversiontable_deploymentid");
        });

        modelBuilder.Entity<Orleansmembershipversiontable>(entity =>
        {
            entity.HasKey(e => e.Deploymentid).HasName("pk_orleansmembershipversiontable_deploymentid");

            entity.ToTable("orleansmembershipversiontable");

            entity.Property(e => e.Deploymentid)
                .HasMaxLength(150)
                .HasColumnName("deploymentid");
            entity.Property(e => e.Timestamp)
                .HasPrecision(3)
                .HasDefaultValueSql("now()")
                .HasColumnName("timestamp");
            entity.Property(e => e.Version).HasColumnName("version");
        });

        modelBuilder.Entity<Orleansquery>(entity =>
        {
            entity.HasKey(e => e.Querykey).HasName("orleansquery_key");

            entity.ToTable("orleansquery");

            entity.Property(e => e.Querykey)
                .HasMaxLength(64)
                .HasColumnName("querykey");
            entity.Property(e => e.Querytext)
                .HasMaxLength(8000)
                .HasColumnName("querytext");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
