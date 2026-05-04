using Lyo.Exceptions;
using Lyo.PackageMetadata.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.PackageMetadata.Postgres;

/// <summary>DI registration for PostgreSQL package metadata.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="PackageMetadataDbContext" /> factory and startup migrations hosting.</summary>
        public IServiceCollection AddPackageMetadataDbContextFactory(Action<PostgresPackageMetadataOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresPackageMetadataOptions();
            configure(options);
            return services.AddPackageMetadataDbContextFactory(options);
        }

        /// <summary>Binds <see cref="PostgresPackageMetadataOptions" /> from configuration.</summary>
        public IServiceCollection AddPackageMetadataDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresPackageMetadataOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresPackageMetadataOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPackageMetadataDbContextFactory(options);
        }

        /// <summary>Adds DbContext factory using the given options.</summary>
        public IServiceCollection AddPackageMetadataDbContextFactory(PostgresPackageMetadataOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<PackageMetadataDbContext, PostgresPackageMetadataOptions>();
            services.AddDbContextFactory<PackageMetadataDbContext>(dbOpts => dbOpts.UseNpgsql(
                options.ConnectionString,
                npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresPackageMetadataOptions.Schema)));

            return services;
        }

        /// <summary>Adds factory and <see cref="IPackageMetadataStore" /> as <see cref="PostgresPackageMetadataStore" />.</summary>
        public IServiceCollection AddPostgresPackageMetadataStore(Action<PostgresPackageMetadataOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresPackageMetadataOptions();
            configure(options);
            return services.AddPostgresPackageMetadataStore(options);
        }

        /// <summary>Adds factory and store from bound configuration.</summary>
        public IServiceCollection AddPostgresPackageMetadataStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresPackageMetadataOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresPackageMetadataOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresPackageMetadataStore(options);
        }

        /// <summary>Adds <see cref="PostgresPackageMetadataStore" /> after the factory is registered.</summary>
        public IServiceCollection AddPostgresPackageMetadataStore(PostgresPackageMetadataOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPackageMetadataDbContextFactory(options);
            services.AddSingleton<IPackageMetadataStore>(sp =>
                new PostgresPackageMetadataStore(
                    sp.GetRequiredService<IDbContextFactory<PackageMetadataDbContext>>(),
                    sp.GetRequiredService<IOptions<PostgresPackageMetadataOptions>>().Value));
            return services;
        }
    }
}
