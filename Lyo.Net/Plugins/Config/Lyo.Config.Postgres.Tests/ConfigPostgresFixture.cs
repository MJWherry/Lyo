using System.Security.Cryptography;
using System.Text;
using Lyo.Config;
using Lyo.Config.Postgres.Database;
using Lyo.Encryption.Extensions;
using Lyo.KeyStore;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Config.Postgres.Tests;

public sealed class ConfigPostgresFixture : PostgresServiceFixtureBase<ConfigDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddPostgresConfigStore(new PostgresConfigOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddKeyedLocalKeyStore(
            ConfigQueryRoutes.EncryptionKeyName, ks => {
                var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-config-values-test-key/v1"));
                ks.AddKey(ConfigQueryRoutes.EncryptionKeyName, "v1", seed);
                ks.SetCurrentVersion(ConfigQueryRoutes.EncryptionKeyName, "v1");
            });

        services.AddEncryptionServiceKeyed(ConfigQueryRoutes.EncryptionKeyName, ConfigQueryRoutes.EncryptionKeyName);
        services.AddConfigValueEncryption(ConfigQueryRoutes.EncryptionKeyName);
    }
}
