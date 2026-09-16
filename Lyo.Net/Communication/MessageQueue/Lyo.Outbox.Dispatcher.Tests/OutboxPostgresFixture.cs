using Lyo.Idempotency.Postgres;
using Lyo.Idempotency.Postgres.Database;
using Lyo.Outbox.Postgres;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lyo.Outbox.Dispatcher.Tests;

[CollectionDefinition("outbox", DisableParallelization = true)]
public sealed class OutboxCollection : ICollectionFixture<OutboxPostgresFixture>;

public sealed class OutboxPostgresFixture : PostgresServiceFixtureBase<IdempotencyDbContext>
{
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddPostgresIdempotency(new PostgresIdempotencyOptions {
            ConnectionString = connectionString,
            EnableAutoMigrations = true
        });
        services.AddPostgresOutbox();
    }

    protected override async ValueTask OnMigratedAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS idem_probe (id uuid PRIMARY KEY)", conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
