using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Story.Postgres.Database;
using Lyo.Story.Postgres.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Story.Postgres;

/// <summary>DI helpers that register the PostgreSQL story store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="StoryDbContext" /> from a configure delegate.</summary>
        public IServiceCollection AddStoryDbContextFactory(Action<PostgresStoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresStoryOptions();
            configure(options);
            return services.AddStoryDbContextFactory(options);
        }

        /// <summary>Registers <see cref="StoryDbContext" /> from configuration section <see cref="PostgresStoryOptions.SectionName" />.</summary>
        public IServiceCollection AddStoryDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresStoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresStoryOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddStoryDbContextFactory(options);
        }

        /// <summary>Registers <see cref="StoryDbContext" /> from an options instance.</summary>
        public IServiceCollection AddStoryDbContextFactory(PostgresStoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<StoryDbContext, PostgresStoryOptions>(options);
            return services;
        }

        /// <summary>Registers <see cref="IStoryStore" /> as <see cref="PostgresStoryStore" /> from a configure delegate.</summary>
        public IServiceCollection AddPostgresStoryStore(Action<PostgresStoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresStoryOptions();
            configure(options);
            return services.AddPostgresStoryStore(options);
        }

        /// <summary>Registers <see cref="IStoryStore" /> from configuration section <see cref="PostgresStoryOptions.SectionName" />.</summary>
        public IServiceCollection AddPostgresStoryStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresStoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresStoryOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresStoryStore(options);
        }

        /// <summary>Registers <see cref="IStoryStore" /> as <see cref="PostgresStoryStore" /> from an options instance.</summary>
        public IServiceCollection AddPostgresStoryStore(PostgresStoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddStoryDbContextFactory(options);
            services.TryAddSingleton<StoryLyoMapper>();
            services.TryAddSingleton<ILyoMapper>(sp => sp.GetRequiredService<StoryLyoMapper>());
            services.AddLyoCrudServices<StoryDbContext>();
            services.TryAddSingleton<IStoryStore, PostgresStoryStore>();
            return services;
        }
    }
}