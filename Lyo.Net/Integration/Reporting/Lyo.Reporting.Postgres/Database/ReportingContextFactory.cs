using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Reporting.Postgres.Database;

/// <summary>Design-time factory for EF Core migrations.</summary>
public sealed class ReportingContextFactory : IDesignTimeDbContextFactory<ReportingContext>
{
    public ReportingContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("REPORTING_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "REPORTING_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ReportingContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresReportingOptions.Schema));
        return new(optionsBuilder.Options);
    }
}