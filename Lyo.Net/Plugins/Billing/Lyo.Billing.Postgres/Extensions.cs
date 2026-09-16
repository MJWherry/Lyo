using Lyo.Billing;
using Lyo.Encryption;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Billing.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Billing.Postgres;

/// <summary>PostgreSQL Billing storage DI registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="IDbContextFactory{TContext}" /> for <see cref="Database.BillingDbContext" />.</summary>
        public IServiceCollection AddBillingDbContextFactory(Action<PostgresBillingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresBillingOptions();
            configure(options);
            return services.AddBillingDbContextFactory(options);
        }

        /// <summary>Registers the Billing context factory from configuration.</summary>
        public IServiceCollection AddBillingDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresBillingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresBillingOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddBillingDbContextFactory(options);
        }

        /// <summary>Registers the Billing context factory.</summary>
        public IServiceCollection AddBillingDbContextFactory(PostgresBillingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<Database.BillingDbContext, PostgresBillingOptions>(options);
            services.AddEntityRefOptions();
            return services;
        }

        /// <summary>Registers <see cref="IBillingFieldEncryptionService" /> against an optional keyed <see cref="IEncryptionService" />.</summary>
        public IServiceCollection AddBillingFieldEncryption(string keyName = BillingEncryptionKeys.Fields)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            services.TryAddSingleton<IBillingFieldEncryptionService>(sp => {
                var encryption = sp.GetKeyedService<IEncryptionService>(keyName) ?? sp.GetService<IEncryptionService>();
                return new BillingFieldEncryptionService(encryption, keyName, sp.GetService<ILogger<BillingFieldEncryptionService>>());
            });
            return services;
        }

        /// <summary>Registers the Billing Postgres store.</summary>
        public IServiceCollection AddPostgresBillingStore(Action<PostgresBillingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresBillingOptions();
            configure(options);
            return services.AddPostgresBillingStore(options);
        }

        /// <summary>Registers the Billing Postgres store from configuration.</summary>
        public IServiceCollection AddPostgresBillingStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresBillingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresBillingOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresBillingStore(options);
        }

        /// <summary>Registers the Billing Postgres store. Hosts must also register <c>ILedgerStore</c>.</summary>
        public IServiceCollection AddPostgresBillingStore(PostgresBillingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(options);
            services.AddBillingDbContextFactory(options);
            services.AddBillingFieldEncryption();
            services.AddSingleton<IBillingStore, PostgresBillingStore>();
            return services;
        }
    }
}
