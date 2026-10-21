using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Mapping;
using Lyo.Exceptions;
using Lyo.Leaderboard.Postgres.Database;
using Lyo.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Leaderboard.Postgres;

/// <summary>DI registration for PostgreSQL leaderboard storage.</summary>
public static class Extensions
{
    /// <summary>Maps Query / QueryProject for entries and standings.</summary>
    public static WebApplication MapLeaderboardQueryEndpoints(this WebApplication app)
    {
        ArgumentHelpers.ThrowIfNull(app);
        app.CreateBuilder<LeaderboardDbContext, EntryEntity, EntryEntity, EntryEntity, Guid>("Leaderboard/Entry", "Leaderboard").WithCrud(ApiFeatureSet.ReadOnly, new()).Build();
        app.CreateBuilder<LeaderboardDbContext, StandingEntity, StandingEntity, StandingEntity, Guid>("Leaderboard/Standing", "Leaderboard")
            .WithCrud(new(ApiFeature.Query), new())
            .Build();

        return app;
    }

    extension(IServiceCollection services)
    {
        /// <summary>Registers the leaderboard DbContext factory from a configure delegate.</summary>
        public IServiceCollection AddLeaderboardDbContextFactory(Action<PostgresLeaderboardOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLeaderboardOptions();
            configure(options);
            return services.AddLeaderboardDbContextFactory(options);
        }

        /// <summary>Registers the leaderboard DbContext factory from configuration.</summary>
        public IServiceCollection AddLeaderboardDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLeaderboardOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresLeaderboardOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddLeaderboardDbContextFactory(options);
        }

        /// <summary>Registers the leaderboard DbContext factory.</summary>
        public IServiceCollection AddLeaderboardDbContextFactory(PostgresLeaderboardOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<LeaderboardDbContext, PostgresLeaderboardOptions>(options);
            return services;
        }

        /// <summary>Registers <see cref="ILeaderboardStore" /> backed by PostgreSQL.</summary>
        public IServiceCollection AddPostgresLeaderboardStore(Action<PostgresLeaderboardOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLeaderboardOptions();
            configure(options);
            return services.AddPostgresLeaderboardStore(options);
        }

        /// <summary>Registers <see cref="ILeaderboardStore" /> from configuration.</summary>
        public IServiceCollection AddPostgresLeaderboardStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLeaderboardOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresLeaderboardOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresLeaderboardStore(options);
        }

        /// <summary>Registers <see cref="ILeaderboardStore" /> and the scoring engine.</summary>
        public IServiceCollection AddPostgresLeaderboardStore(PostgresLeaderboardOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddLeaderboardDbContextFactory(options);
            services.AddSingleton<ILeaderboardStore, PostgresLeaderboardStore>();
            services.TryAddSingleton<ILyoMapper, LeaderboardIdentityLyoMapper>();
            return services;
        }

        /// <summary>Registers query/CRUD-read services for <see cref="LeaderboardDbContext" />. Requires <c>AddLyoQueryServices</c> and a cache.</summary>
        public IServiceCollection AddLeaderboardQueryServices()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddLyoCrudServices<LeaderboardDbContext>();
            services.TryAddSingleton<ILyoMapper, LeaderboardIdentityLyoMapper>();
            return services;
        }
    }
}

/// <summary>Identity mapper so query endpoints can use entity-as-both DTOs.</summary>
internal sealed class LeaderboardIdentityLyoMapper : ILyoMapper
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