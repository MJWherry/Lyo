using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Configuration;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.ChangeTracker.Postgres;

/// <summary>DI helpers that register PostgreSQL change tracking.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="ChangeTrackerDbContext" /> on the collection.</summary>
        public IServiceCollection AddChangeTrackerDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddPostgresChangeTracker(new PostgresChangeTrackerOptions { ConnectionString = connectionString })
                .AddScoped<ChangeTrackerDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>().CreateDbContext());
        }

        /// <summary>Registers a change-tracker <see cref="IDbContextFactory{TContext}" />.</summary>
        public IServiceCollection AddChangeTrackerDbContextFactory(Action<PostgresChangeTrackerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresChangeTrackerOptions();
            configure(options);
            return services.AddChangeTrackerDbContextFactory(options);
        }

        /// <summary>Registers a change-tracker <see cref="IDbContextFactory{TContext}" /> from configuration.</summary>
        public IServiceCollection AddChangeTrackerDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresChangeTrackerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresChangeTrackerOptions>(configuration, configSectionName);

            return services.AddChangeTrackerDbContextFactory(options);
        }

        /// <summary>Registers a change-tracker <see cref="IDbContextFactory{TContext}" />.</summary>
        public IServiceCollection AddChangeTrackerDbContextFactory(PostgresChangeTrackerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<ChangeTrackerDbContext, PostgresChangeTrackerOptions>(options);

            return services;
        }

        /// <summary>Registers PostgreSQL change-tracking services.</summary>
        public IServiceCollection AddPostgresChangeTracker(Action<PostgresChangeTrackerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresChangeTrackerOptions();
            configure(options);
            return services.AddPostgresChangeTracker(options);
        }

        /// <summary>Registers PostgreSQL change-tracking services from configuration.</summary>
        public IServiceCollection AddPostgresChangeTrackerFromConfiguration(IConfiguration configuration, string configSectionName = PostgresChangeTrackerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresChangeTrackerOptions>(configuration, configSectionName);

            return services.AddPostgresChangeTracker(options);
        }

        /// <summary>Registers PostgreSQL change-tracking services.</summary>
        public IServiceCollection AddPostgresChangeTracker(PostgresChangeTrackerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddChangeTrackerDbContextFactory(options);
            services.AddEntityRefOptions();
            services.AddSingleton<IChangeTracker, PostgresChangeTracker>();
            return services;
        }
    }
}