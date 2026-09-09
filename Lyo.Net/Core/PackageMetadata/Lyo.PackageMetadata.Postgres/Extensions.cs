using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.PackageMetadata.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.PackageMetadata.Postgres;

/// <summary>Helpers that register PostgreSQL package metadata.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the <see cref="PackageMetadataDbContext" /> factory and startup migrations hosting.</summary>
        public IServiceCollection AddPackageMetadataDbContextFactory(Action<PostgresPackageMetadataOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresPackageMetadataOptions();
            configure(options);
            return services.AddPackageMetadataDbContextFactory(options);
        }

        /// <summary>Binds <see cref="PostgresPackageMetadataOptions" /> from configuration and registers the factory.</summary>
        public IServiceCollection AddPackageMetadataDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresPackageMetadataOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresPackageMetadataOptions>(configuration, configSectionName);

            return services.AddPackageMetadataDbContextFactory(options);
        }

        /// <summary>Registers the DbContext factory from the given options.</summary>
        public IServiceCollection AddPackageMetadataDbContextFactory(PostgresPackageMetadataOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<PackageMetadataDbContext, PostgresPackageMetadataOptions>(options);

            return services;
        }

        /// <summary>Registers the factory and <see cref="IPackageMetadataStore" /> as <see cref="PostgresPackageMetadataStore" />.</summary>
        public IServiceCollection AddPostgresPackageMetadataStore(Action<PostgresPackageMetadataOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresPackageMetadataOptions();
            configure(options);
            return services.AddPostgresPackageMetadataStore(options);
        }

        /// <summary>Registers the factory and store from bound configuration.</summary>
        public IServiceCollection AddPostgresPackageMetadataStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresPackageMetadataOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresPackageMetadataOptions>(configuration, configSectionName);

            return services.AddPostgresPackageMetadataStore(options);
        }

        /// <summary>Registers <see cref="PostgresPackageMetadataStore" /> after the factory is in place.</summary>
        public IServiceCollection AddPostgresPackageMetadataStore(PostgresPackageMetadataOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPackageMetadataDbContextFactory(options);
            services.AddSingleton<IPackageMetadataStore>(sp => new PostgresPackageMetadataStore(
                sp.GetRequiredService<IDbContextFactory<PackageMetadataDbContext>>(), sp.GetRequiredService<IOptions<PostgresPackageMetadataOptions>>().Value));

            return services;
        }
    }
}