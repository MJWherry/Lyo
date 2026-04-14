using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.ChangeTracker.Postgres.Database;

/// <summary>Design-time factory for creating ChangeTrackerDbContext instances for migrations.</summary>
public class ChangeTrackerDbContextFactory : IDesignTimeDbContextFactory<ChangeTrackerDbContext>
{
    public ChangeTrackerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CHANGE_TRACKER_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "CHANGE_TRACKER_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ChangeTrackerDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresChangeTrackerOptions.Schema));
        return new(optionsBuilder.Options);
    }
}