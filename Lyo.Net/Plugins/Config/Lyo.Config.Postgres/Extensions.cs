using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Mapping;
using Lyo.Configuration;
using Lyo.Config;
using Lyo.Config.Postgres.Database;
using Lyo.Encryption;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
namespace Lyo.Config.Postgres;

/// <summary>DI helpers for PostgreSQL config store registration.</summary>
public static class Extensions
{
    private static readonly string[] DefinitionDeniedSelectFields = ["DefaultValueJson", "EncryptedDefaultValue"];

    /// <summary>Wires query/get/export endpoints for config definitions and bindings. Writes stay on <see cref="IConfigStore" />.</summary>
    public static WebApplication MapConfigQueryEndpoints(this WebApplication app)
    {
        ArgumentHelpers.ThrowIfNull(app);
        app.CreateBuilder<ConfigDbContext, ConfigDefinitionEntity, ConfigDefinitionEntity, ConfigDefinitionEntity, Guid>(ConfigQueryRoutes.Definition, ConfigQueryRoutes.Group)
            .WithCrud(
                ApiFeatureSet.ReadOnly + ExportApiFeature.Instance, new() {
                    DeniedSelectFields = DefinitionDeniedSelectFields
                })
            .Build();

        app.CreateBuilder<ConfigDbContext, ConfigBindingEntity, ConfigBindingEntity, ConfigBindingEntity, Guid>(ConfigQueryRoutes.Binding, ConfigQueryRoutes.Group)
            .WithCrud(ApiFeatureSet.ReadOnly + ExportApiFeature.Instance, new())
            .Build();

        return app;
    }

    extension(IServiceCollection services)
    {
        /// <summary>Registers PostgreSQL config DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddConfigDbContextFactory(Action<PostgresConfigOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresConfigOptions();
            configure(options);
            return services.AddConfigDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL config DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddConfigDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresConfigOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresConfigOptions>(configuration, configSectionName);

            return services.AddConfigDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL config DbContextFactory to the service collection.</summary>
        public IServiceCollection AddConfigDbContextFactory(PostgresConfigOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<ConfigDbContext, PostgresConfigOptions>(options);
            services.AddEntityRefOptions();

            return services;
        }

        /// <summary>Registers PostgreSQL config store registration using configuration.</summary>
        public IServiceCollection AddPostgresConfigStore(Action<PostgresConfigOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresConfigOptions();
            configure(options);
            return services.AddPostgresConfigStore(options);
        }

        /// <summary>Registers PostgreSQL config store registration using configuration binding.</summary>
        public IServiceCollection AddPostgresConfigStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresConfigOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresConfigOptions>(configuration, configSectionName);

            return services.AddPostgresConfigStore(options);
        }

        /// <summary>Registers PostgreSQL config store registration.</summary>
        public IServiceCollection AddPostgresConfigStore(PostgresConfigOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddConfigDbContextFactory(options);
            services.AddSingleton<IConfigStore>(sp => new PostgresConfigStore(
                sp.GetRequiredService<IDbContextFactory<ConfigDbContext>>(),
                sp.GetRequiredService<EntityRefOptions>(),
                sp.GetRequiredService<PostgresConfigOptions>(),
                sp.GetService<IConfigValueEncryptionService>()));
            services.TryAddSingleton<ILyoMapper, ConfigIdentityLyoMapper>();
            return services;
        }

        /// <summary>
        /// Adds query/CRUD/export services for <see cref="ConfigDbContext" />. Call on API hosts before <see cref="MapConfigQueryEndpoints" />.
        /// Needs <c>AddLyoQueryServices</c> and a cache implementation.
        /// </summary>
        public IServiceCollection AddConfigQueryServices()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddLyoCrudServices<ConfigDbContext>();
            services.AddLyoApiExport<ConfigDbContext>();
            services.TryAddSingleton<ILyoMapper, ConfigIdentityLyoMapper>();
            return services;
        }

        /// <summary>Adds <see cref="ConfigValueEncryptionService" /> using an optional keyed <see cref="IEncryptionService" />.</summary>
        /// <param name="keyName">Keyed name for <see cref="IEncryptionService" /> and the key id given to EncryptString.</param>
        public IServiceCollection AddConfigValueEncryption(string keyName = ConfigQueryRoutes.EncryptionKeyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            services.TryAddSingleton<IConfigValueEncryptionService>(sp => {
                var encryption = sp.GetKeyedService<IEncryptionService>(keyName) ?? sp.GetService<IEncryptionService>();
                return new ConfigValueEncryptionService(encryption, keyName, sp.GetService<ILogger<ConfigValueEncryptionService>>());
            });

            return services;
        }
    }
}

/// <summary>Passthrough mapper so query endpoints can use entity-as-both DTOs. Mapping is skipped when source and dest types match.</summary>
internal sealed class ConfigIdentityLyoMapper : ILyoMapper
{
    public TResult Map<TResult>(object source)
    {
        if (source is TResult typed)
            return typed;

        throw new InvalidOperationException($"No mapping from {source.GetType().Name} to {typeof(TResult).Name}.");
    }

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        if (source is TDest)
            return;

        throw new InvalidOperationException($"No mapping from {typeof(TSource).Name} to {typeof(TDest).Name}.");
    }
}