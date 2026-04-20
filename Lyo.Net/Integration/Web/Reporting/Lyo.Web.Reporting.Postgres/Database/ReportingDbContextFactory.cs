using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Web.Reporting.Postgres.Database;

/// <summary>Design-time factory for creating ReportingDbContext instances for migrations.</summary>
public class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        // Connection string must be provided via environment variable for design-time operations
        var connectionString = Environment.GetEnvironmentVariable("REPORTING_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "REPORTING_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ReportingDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "report"));
        return new(optionsBuilder.Options);
    }
}