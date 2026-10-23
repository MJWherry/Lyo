using Lyo.Testing.Containers;
using Lyo.Web.Automation.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Postgres.Tests;

public sealed class AutomationPlanPostgresFixture : PostgresServiceFixtureBase<AutomationPlanDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddAutomationPlansPostgres(new PostgresAutomationPlanOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}