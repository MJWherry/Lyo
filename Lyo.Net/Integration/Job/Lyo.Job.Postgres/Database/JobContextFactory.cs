using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Job.Postgres.Database;

/// <summary>Design-time factory for EF Core migrations.</summary>
public sealed class JobContextFactory : IDesignTimeDbContextFactory<JobContext>
{
    public JobContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("JOB_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "JOB_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<JobContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "job"));
        return new(optionsBuilder.Options);
    }
}