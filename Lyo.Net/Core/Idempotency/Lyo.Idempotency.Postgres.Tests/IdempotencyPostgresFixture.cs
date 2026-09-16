using Lyo.Idempotency.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lyo.Idempotency.Postgres.Tests;

public sealed class IdempotencyPostgresFixture : PostgresServiceFixtureBase<IdempotencyDbContext>
{
    public PostgresIdempotencyStore Store => ServiceProvider.GetRequiredService<PostgresIdempotencyStore>();

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddPostgresIdempotency(new PostgresIdempotencyOptions {
            ConnectionString = connectionString,
            EnableAutoMigrations = true
        });
    }

    protected override async ValueTask OnMigratedAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS idem_probe (id uuid PRIMARY KEY)", conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
