using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Sms.Twilio.Postgres.Tests;

public sealed class TwilioPostgresFixture : PostgresServiceFixtureBase<TwilioSmsDbContext>
{
    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
