using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Seed.Tests;

public sealed class SeedHarnessDbContext : DbContext
{
    public DbSet<ParentRow> Parents => Set<ParentRow>();

    public DbSet<ChildRow> Children => Set<ChildRow>();

    public SeedHarnessDbContext(DbContextOptions<SeedHarnessDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParentRow>().HasKey(p => p.Id);
        modelBuilder.Entity<ChildRow>().HasKey(c => c.Id);
        modelBuilder.Entity<ChildRow>().HasOne<ParentRow>().WithMany().HasForeignKey(c => c.ParentId);
    }
}

public sealed class ParentRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";
}

public sealed class ChildRow
{
    public Guid Id { get; set; }

    public Guid ParentId { get; set; }

    public string Label { get; set; } = "";
}
