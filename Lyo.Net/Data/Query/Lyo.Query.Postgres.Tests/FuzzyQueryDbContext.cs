using Microsoft.EntityFrameworkCore;

namespace Lyo.Query.Postgres.Tests;

public sealed class FuzzyPersonEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class FuzzyQueryDbContext(DbContextOptions<FuzzyQueryDbContext> options) : DbContext(options)
{
    public DbSet<FuzzyPersonEntity> People => Set<FuzzyPersonEntity>();
}
