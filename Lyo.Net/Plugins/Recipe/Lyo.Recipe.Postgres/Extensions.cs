using Lyo.Api.Mapping;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Postgres;
using Lyo.Recipe.Postgres.Database;
using Lyo.Recipe.Postgres.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Recipe.Postgres;

/// <summary>PostgreSQL recipe storage DI registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddRecipeDbContextFactory(Action<PostgresRecipeOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresRecipeOptions();
            configure(options);
            return services.AddRecipeDbContextFactory(options);
        }

        public IServiceCollection AddRecipeDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresRecipeOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresRecipeOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddRecipeDbContextFactory(options);
        }

        public IServiceCollection AddRecipeDbContextFactory(PostgresRecipeOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<RecipeDbContext, PostgresRecipeOptions>(options);
            services.AddEntityRefOptions();
            return services;
        }

        public IServiceCollection AddPostgresRecipeStore(Action<PostgresRecipeOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresRecipeOptions();
            configure(options);
            return services.AddPostgresRecipeStore(options);
        }

        public IServiceCollection AddPostgresRecipeStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresRecipeOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresRecipeOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresRecipeStore(options);
        }

        public IServiceCollection AddPostgresRecipeStore(PostgresRecipeOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddRecipeDbContextFactory(options);
            services.AddSingleton<IRecipeStore, PostgresRecipeStore>();
            services.AddSingleton<IHealth>(sp => (IHealth)sp.GetRequiredService<IRecipeStore>());
            services.TryAddSingleton<RecipeLyoMapper>();
            services.TryAddSingleton<ILyoMapper>(sp => sp.GetRequiredService<RecipeLyoMapper>());
            return services;
        }
    }
}
