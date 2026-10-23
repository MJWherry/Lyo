using System.Security.Cryptography;
using System.Text;
using Lyo.Billing.Postgres.Database;
using Lyo.Encryption.Extensions;
using Lyo.KeyStore;
using Lyo.Ledger.Postgres;
using Lyo.Ledger.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Billing.Postgres.Tests;

/// <summary>Testcontainers fixture for billing plus ledger schemas.</summary>
public sealed class BillingPostgresFixture : PostgresServiceFixtureBase<BillingDbContext>
{
    /// <inheritdoc />
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddPostgresLedgerStore(new PostgresLedgerOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddPostgresBillingStore(new PostgresBillingOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        AddKey(services, BillingEncryptionKeys.Fields, "lyo-billing-fields-test-key/v1");
        services.AddEncryptionServiceKeyed(BillingEncryptionKeys.Fields, BillingEncryptionKeys.Fields);
        services.AddBillingFieldEncryption();
    }

    /// <inheritdoc />
    protected override async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        await base.MigrateAsync(cancellationToken).ConfigureAwait(false);
        using var scope = CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LedgerDbContext>>();
        await using (var context = await ledger.CreateDbContextAsync(cancellationToken))
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddKey(IServiceCollection services, string name, string seedText)
        => services.AddKeyedLocalKeyStore(
            name, ks => {
                var seed = SHA256.HashData(Encoding.UTF8.GetBytes(seedText));
                ks.AddKey(name, "v1", seed);
                ks.SetCurrentVersion(name, "v1");
            });
}