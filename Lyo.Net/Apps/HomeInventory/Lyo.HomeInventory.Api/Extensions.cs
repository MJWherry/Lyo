using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Mapping;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.HomeInventory.Postgres;
using Lyo.HomeInventory.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.HomeInventory.Api;

/// <summary>HTTP mapping for the Lyo HomeInventory API. Register store DI with <c>AddHomeInventoryApiFromConfiguration</c> first.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the HomeInventory Postgres store, CRUD services, and an identity mapper for entity-as-both endpoints.</summary>
        public IServiceCollection AddHomeInventoryApi(Action<PostgresHomeInventoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresHomeInventoryOptions();
            configure(options);
            return services.AddHomeInventoryApi(options);
        }

        /// <summary>Registers HomeInventory from configuration section <see cref="PostgresHomeInventoryOptions.SectionName" />.</summary>
        public IServiceCollection AddHomeInventoryApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresHomeInventoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddPostgresHomeInventoryStoreFromConfiguration(configuration, configSectionName);
            return services.AddHomeInventoryApiCore();
        }

        /// <summary>Registers the HomeInventory Postgres store, CRUD services, and an identity mapper for entity-as-both endpoints.</summary>
        public IServiceCollection AddHomeInventoryApi(PostgresHomeInventoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresHomeInventoryStore(options);
            return services.AddHomeInventoryApiCore();
        }

        private IServiceCollection AddHomeInventoryApiCore()
        {
            services.AddLyoCrudServices<HomeInventoryDbContext>();
            services.TryAddSingleton<ILyoMapper, IdentityLyoMapper>();
            return services;
        }
    }

    /// <summary>Maps HomeInventory CRUD endpoints. Invoke after <c>AddHomeInventoryApi</c>. Stock (composite key) is not mapped.</summary>
    public static WebApplication BuildHomeInventoryGroup(this WebApplication app)
    {
        app.CreateBuilder<HomeInventoryDbContext, HomeCategoryEntity, HomeCategoryEntity, HomeCategoryEntity, Guid>("HomeInventory/Categories", "HomeInventory")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<HomeInventoryDbContext, HomeLocationEntity, HomeLocationEntity, HomeLocationEntity, Guid>("HomeInventory/Locations", "HomeInventory")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<HomeInventoryDbContext, HomeItemEntity, HomeItemEntity, HomeItemEntity, Guid>("HomeInventory/Items", "HomeInventory")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<HomeInventoryDbContext, HomeItemMovementEntity, HomeItemMovementEntity, HomeItemMovementEntity, Guid>("HomeInventory/Movements", "HomeInventory")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        return app;
    }
}

/// <summary>Passthrough mapper so query endpoints can use entity-as-both DTOs. Mapping is skipped when source and dest types match.</summary>
internal sealed class IdentityLyoMapper : ILyoMapper
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
