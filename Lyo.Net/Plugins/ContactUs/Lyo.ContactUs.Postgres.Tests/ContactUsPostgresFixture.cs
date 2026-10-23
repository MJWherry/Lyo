using Lyo.ContactUs.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.ContactUs.Postgres.Tests;

public sealed class ContactUsPostgresFixture : PostgresServiceFixtureBase<ContactUsDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddContactUsPostgres(new PostgresContactUsOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}