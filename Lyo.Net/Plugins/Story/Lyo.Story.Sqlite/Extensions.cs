using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Sqlite;
using Lyo.Story.Sqlite.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Story.Sqlite;

/// <summary>DI registration for the SQLite story store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers only the <c>IDbContextFactory&lt;StoryDbContext&gt;</c>. Useful for tooling and migrations.</summary>
        public IServiceCollection AddSqliteStoryDbContextFactory(Action<SqliteStoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SqliteStoryOptions();
            configure(options);
            return services.AddSqliteStoryDbContextFactory(options);
        }

        /// <summary>Registers only the context factory, bound from <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddSqliteStoryDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = SqliteStoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new SqliteStoryOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddSqliteStoryDbContextFactory(options);
        }

        /// <summary>Registers only the context factory. Validates <paramref name="options" /> first.</summary>
        public IServiceCollection AddSqliteStoryDbContextFactory(SqliteStoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.AddSqliteMigrations<StoryDbContext, SqliteStoryOptions>();
            services.AddDbContextFactory<StoryDbContext>(db => db.UseSqlite(options.ConnectionString));
            return services;
        }

        /// <summary>Registers the context factory and <see cref="IStoryStore" />.</summary>
        public IServiceCollection AddSqliteStoryStore(Action<SqliteStoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SqliteStoryOptions();
            configure(options);
            return services.AddSqliteStoryStore(options);
        }

        /// <summary>Registers the context factory and <see cref="IStoryStore" />, bound from <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddSqliteStoryStoreFromConfiguration(IConfiguration configuration, string configSectionName = SqliteStoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new SqliteStoryOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddSqliteStoryStore(options);
        }

        /// <summary>Registers the context factory and <see cref="IStoryStore" />. The store also registers as <see cref="IHealth" />.</summary>
        public IServiceCollection AddSqliteStoryStore(SqliteStoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSqliteStoryDbContextFactory(options);
            services.TryAddSingleton<SqliteStoryStore>();
            services.TryAddSingleton<IStoryStore>(sp => sp.GetRequiredService<SqliteStoryStore>());
            services.AddSingleton<IHealth>(sp => sp.GetRequiredService<SqliteStoryStore>());
            return services;
        }
    }

    extension(IServiceProvider provider)
    {
        /// <summary>Creates or upgrades the SQLite database. Call once at startup when there is no generic host to run the migration service, for example in a game engine.</summary>
        public async Task MigrateSqliteStoryAsync(CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(provider);
            await using var context = await provider.GetRequiredService<IDbContextFactory<StoryDbContext>>().CreateDbContextAsync(ct).ConfigureAwait(false);
            await context.Database.MigrateAsync(ct).ConfigureAwait(false);
        }
    }
}
