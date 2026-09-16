using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Web.Automation.Postgres.Database;

/// <summary>Design-time factory for <c>dotnet ef migrations</c>.</summary>
public sealed class AutomationPlanDbContextFactory : IDesignTimeDbContextFactory<AutomationPlanDbContext>
{
    /// <inheritdoc />
    public AutomationPlanDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<AutomationPlanDbContext>("AUTOMATION_CONNECTION_STRING", PostgresAutomationPlanOptions.Schema));
}
