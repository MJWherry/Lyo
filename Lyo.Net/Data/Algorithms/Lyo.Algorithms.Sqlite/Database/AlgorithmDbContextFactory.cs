using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Algorithms.Sqlite.Database;

/// <summary>Design-time factory for <see cref="AlgorithmDbContext" /> migrations.</summary>
public sealed class AlgorithmDbContextFactory : IDesignTimeDbContextFactory<AlgorithmDbContext>
{
    /// <inheritdoc />
    public AlgorithmDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ALGORITHMS_CONNECTION_STRING") ?? "Data Source=./algorithms-design.db";
        var builder = new DbContextOptionsBuilder<AlgorithmDbContext>();
        builder.UseSqlite(connectionString);
        return new(builder.Options);
    }
}