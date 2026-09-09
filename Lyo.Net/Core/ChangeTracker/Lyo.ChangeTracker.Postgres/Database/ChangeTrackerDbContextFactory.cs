using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.ChangeTracker.Postgres.Database;

/// <summary>Design-time factory that builds <see cref="ChangeTrackerDbContext" /> for EF migrations.</summary>
public class ChangeTrackerDbContextFactory : IDesignTimeDbContextFactory<ChangeTrackerDbContext>
{
    /// <inheritdoc />
    public ChangeTrackerDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<ChangeTrackerDbContext>("CHANGE_TRACKER_CONNECTION_STRING", PostgresChangeTrackerOptions.Schema));
}