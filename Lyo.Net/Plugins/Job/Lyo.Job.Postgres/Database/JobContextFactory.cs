using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Job.Postgres.Database;

/// <summary>Design-time factory used for EF Core migrations.</summary>
public sealed class JobContextFactory : IDesignTimeDbContextFactory<JobContext>
{
    /// <inheritdoc />
    public JobContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<JobContext>("JOB_CONNECTION_STRING", "job"));
}