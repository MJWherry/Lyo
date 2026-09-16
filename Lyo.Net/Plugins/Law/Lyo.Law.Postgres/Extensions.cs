using Lyo.Api.Mapping;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Law.Postgres.Database;
using Lyo.Law.Postgres.Mapping;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Law.Postgres;

/// <summary>PostgreSQL state-statute storage DI registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the law DbContext factory.</summary>
        public IServiceCollection AddLawDbContextFactory(Action<PostgresLawOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLawOptions();
            configure(options);
            return services.AddLawDbContextFactory(options);
        }

        /// <summary>Registers the law DbContext factory from configuration section <see cref="PostgresLawOptions.SectionName"/>.</summary>
        public IServiceCollection AddLawDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLawOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresLawOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddLawDbContextFactory(options);
        }

        /// <summary>Registers the law DbContext factory.</summary>
        public IServiceCollection AddLawDbContextFactory(PostgresLawOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<LawDbContext, PostgresLawOptions>(options);
            return services;
        }

        /// <summary>Registers <see cref="ILawStore"/> on PostgreSQL.</summary>
        public IServiceCollection AddPostgresLawStore(Action<PostgresLawOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLawOptions();
            configure(options);
            return services.AddPostgresLawStore(options);
        }

        /// <summary>Registers <see cref="ILawStore"/> from configuration section <see cref="PostgresLawOptions.SectionName"/>.</summary>
        public IServiceCollection AddPostgresLawStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLawOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresLawOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresLawStore(options);
        }

        /// <summary>Registers <see cref="ILawStore"/> on PostgreSQL.</summary>
        public IServiceCollection AddPostgresLawStore(PostgresLawOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddLawDbContextFactory(options);
            services.AddSingleton<ILawStore, PostgresLawStore>();
            services.AddSingleton<IHealth>(sp => (IHealth)sp.GetRequiredService<ILawStore>());
            services.TryAddSingleton<LawLyoMapper>();
            services.TryAddSingleton<ILyoMapper>(sp => sp.GetRequiredService<LawLyoMapper>());
            return services;
        }
    }
}
