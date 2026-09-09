using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Mapping;
using Lyo.Comic.Postgres;
using Lyo.Comic.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Comic.Api;

/// <summary>HTTP mapping for the Lyo Comic API. Register store DI with <c>AddComicApiFromConfiguration</c> (or the Postgres store plus CRUD services) first.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Comic Postgres store, CRUD services, and an identity mapper for entity-as-both endpoints.</summary>
        public IServiceCollection AddComicApi(Action<PostgresComicOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresComicOptions();
            configure(options);
            return services.AddComicApi(options);
        }

        /// <summary>Registers Comic from configuration section <see cref="PostgresComicOptions.SectionName" />.</summary>
        public IServiceCollection AddComicApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresComicOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddPostgresComicStoreFromConfiguration(configuration, configSectionName);
            return services.AddComicApiCore();
        }

        /// <summary>Registers the Comic Postgres store, CRUD services, and an identity mapper for entity-as-both endpoints.</summary>
        public IServiceCollection AddComicApi(PostgresComicOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresComicStore(options);
            return services.AddComicApiCore();
        }

        private IServiceCollection AddComicApiCore()
        {
            services.AddLyoCrudServices<ComicDbContext>();
            services.TryAddSingleton<ILyoMapper, IdentityLyoMapper>();
            return services;
        }
    }

    /// <summary>Maps Comic CRUD endpoints. Invoke after <c>AddComicApi</c> or equivalent store + <c>AddLyoCrudServices&lt;ComicDbContext&gt;</c>.</summary>
    public static WebApplication BuildComicGroup(this WebApplication app)
    {
        app.CreateBuilder<ComicDbContext, SeriesEntity, SeriesEntity, SeriesEntity, Guid>("Comic/Series", "Comic")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<ComicDbContext, AlternateTitleEntity, AlternateTitleEntity, AlternateTitleEntity, Guid>("Comic/AlternateTitles", "Comic")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<ComicDbContext, VolumeEntity, VolumeEntity, VolumeEntity, Guid>("Comic/Volumes", "Comic")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<ComicDbContext, ChapterEntity, ChapterEntity, ChapterEntity, Guid>("Comic/Chapters", "Comic")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<ComicDbContext, PageEntity, PageEntity, PageEntity, Guid>("Comic/Pages", "Comic")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        app.CreateBuilder<ComicDbContext, CharacterEntity, CharacterEntity, CharacterEntity, Guid>("Comic/Characters", "Comic")
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
