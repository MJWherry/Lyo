using Lyo.Comment.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Comment.Postgres;

/// <summary>DI helpers for PostgreSQL comment store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers PostgreSQL comment DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddCommentDbContextFactory(Action<PostgresCommentOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresCommentOptions();
            configure(options);
            return services.AddCommentDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL comment DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddCommentDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresCommentOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresCommentOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddCommentDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL comment DbContextFactory to the service collection.</summary>
        public IServiceCollection AddCommentDbContextFactory(PostgresCommentOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<CommentDbContext, PostgresCommentOptions>(options);
            services.AddEntityRefOptions();

            return services;
        }

        /// <summary>Registers PostgreSQL comment DbContextFactory and PostgresCommentStore (ICommentStore) to the service collection.</summary>
        public IServiceCollection AddPostgresCommentStore(Action<PostgresCommentOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresCommentOptions();
            configure(options);
            return services.AddPostgresCommentStore(options);
        }

        /// <summary>Registers PostgreSQL comment store using configuration binding.</summary>
        public IServiceCollection AddPostgresCommentStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresCommentOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresCommentOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddPostgresCommentStore(options);
        }

        /// <summary>Registers PostgreSQL comment DbContextFactory and PostgresCommentStore to the service collection.</summary>
        public IServiceCollection AddPostgresCommentStore(PostgresCommentOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddCommentDbContextFactory(options);
            services.AddSingleton<ICommentStore>(sp => new PostgresCommentStore(
                sp.GetRequiredService<IDbContextFactory<CommentDbContext>>(), sp.GetRequiredService<EntityRefOptions>(),
                sp.GetRequiredService<PostgresCommentOptions>(), sp.GetServices<IEntityRefActionInterceptor>()));

            return services;
        }
    }
}