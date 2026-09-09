using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Reporting.Postgres.Database;

/// <summary>Design-time factory used for EF Core migrations.</summary>
public sealed class ReportingContextFactory : IDesignTimeDbContextFactory<ReportingContext>
{
    /// <inheritdoc />
    public ReportingContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<ReportingContext>("REPORTING_CONNECTION_STRING", PostgresReportingOptions.Schema));
}