using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Ledger.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Ledger.Postgres;

/// <summary>PostgreSQL Ledger storage DI registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="IDbContextFactory{TContext}" /> for <see cref="Database.LedgerDbContext" />.</summary>
        public IServiceCollection AddLedgerDbContextFactory(Action<PostgresLedgerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLedgerOptions();
            configure(options);
            return services.AddLedgerDbContextFactory(options);
        }

        /// <summary>Registers the Ledger context factory from configuration.</summary>
        public IServiceCollection AddLedgerDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLedgerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresLedgerOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddLedgerDbContextFactory(options);
        }

        /// <summary>Registers the Ledger context factory.</summary>
        public IServiceCollection AddLedgerDbContextFactory(PostgresLedgerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<LedgerDbContext, PostgresLedgerOptions>(options);
            services.AddEntityRefOptions();
            return services;
        }

        /// <summary>Registers the Ledger Postgres store.</summary>
        public IServiceCollection AddPostgresLedgerStore(Action<PostgresLedgerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLedgerOptions();
            configure(options);
            return services.AddPostgresLedgerStore(options);
        }

        /// <summary>Registers the Ledger Postgres store from configuration.</summary>
        public IServiceCollection AddPostgresLedgerStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLedgerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresLedgerOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresLedgerStore(options);
        }

        /// <summary>Registers the Ledger Postgres store.</summary>
        public IServiceCollection AddPostgresLedgerStore(PostgresLedgerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(options);
            services.AddLedgerDbContextFactory(options);
            services.AddSingleton<ILedgerStore, PostgresLedgerStore>();
            return services;
        }
    }
}