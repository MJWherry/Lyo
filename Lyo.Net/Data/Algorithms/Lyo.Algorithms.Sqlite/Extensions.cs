using Lyo.Algorithms.Sqlite.Database;
using Lyo.Exceptions;
using Lyo.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Algorithms.Sqlite;

/// <summary>DI helpers for the SQLite algorithm catalog.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="AlgorithmDbContext" /> from <paramref name="configure" />.</summary>
        public IServiceCollection AddSqliteAlgorithmDbContextFactory(Action<SqliteAlgorithmOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SqliteAlgorithmOptions();
            configure(options);
            return services.AddSqliteAlgorithmDbContextFactory(options);
        }

        /// <summary>Registers <see cref="AlgorithmDbContext" /> from configuration section <see cref="SqliteAlgorithmOptions.SectionName" />.</summary>
        public IServiceCollection AddSqliteAlgorithmDbContextFactoryFromConfiguration(IConfiguration configuration, string sectionName = SqliteAlgorithmOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new SqliteAlgorithmOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddSqliteAlgorithmDbContextFactory(options);
        }

        /// <summary>Registers <see cref="AlgorithmDbContext" /> from an options instance.</summary>
        public IServiceCollection AddSqliteAlgorithmDbContextFactory(SqliteAlgorithmOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.AddSqliteMigrations<AlgorithmDbContext, SqliteAlgorithmOptions>();
            services.AddDbContextFactory<AlgorithmDbContext>(db => db.UseSqlite(options.ConnectionString));
            return services;
        }

        /// <summary>Registers <see cref="IAlgorithmDefinitionStore" /> as <see cref="SqliteAlgorithmDefinitionStore" />.</summary>
        public IServiceCollection AddSqliteAlgorithmStore(Action<SqliteAlgorithmOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SqliteAlgorithmOptions();
            configure(options);
            return services.AddSqliteAlgorithmStore(options);
        }

        /// <summary>Registers the SQLite catalog from configuration.</summary>
        public IServiceCollection AddSqliteAlgorithmStoreFromConfiguration(IConfiguration configuration, string sectionName = SqliteAlgorithmOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new SqliteAlgorithmOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddSqliteAlgorithmStore(options);
        }

        /// <summary>Registers the SQLite catalog from an options instance. Also registers <see cref="Lyo.Health.IHealth" />.</summary>
        public IServiceCollection AddSqliteAlgorithmStore(SqliteAlgorithmOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSqliteAlgorithmDbContextFactory(options);
            services.AddSingleton<SqliteAlgorithmDefinitionStore>();
            services.AddSingleton<IAlgorithmDefinitionStore>(sp => sp.GetRequiredService<SqliteAlgorithmDefinitionStore>());
            services.AddSingleton<Lyo.Health.IHealth>(sp => sp.GetRequiredService<SqliteAlgorithmDefinitionStore>());
            return services;
        }
    }
}
