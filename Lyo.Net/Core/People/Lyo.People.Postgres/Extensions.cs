using Lyo.Exceptions;
using Lyo.People.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.People.Postgres;

/// <summary>DI helpers that register the PostgreSQL People <c>DbContext</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection to register into.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a scoped <see cref="PeopleDbContext" /> from a connection string.</summary>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddPeopleDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connectionString })
                .AddScoped<PeopleDbContext>(sp => sp.GetRequiredService<IDbContextFactory<PeopleDbContext>>().CreateDbContext());
        }

        /// <summary>Registers a People <c>IDbContextFactory</c> using a configure callback.</summary>
        /// <param name="configure">Callback that fills <see cref="PostgresPeopleOptions" />.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddPeopleDbContextFactory(Action<PostgresPeopleOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresPeopleOptions();
            configure(options);
            return services.AddPeopleDbContextFactory(options);
        }

        /// <summary>Registers a People <c>IDbContextFactory</c> by binding options from configuration.</summary>
        /// <param name="configuration">Host configuration (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section name; defaults to <see cref="PostgresPeopleOptions.SectionName" />.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddPeopleDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresPeopleOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresPeopleOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddPeopleDbContextFactory(options);
        }

        /// <summary>Registers a People <c>IDbContextFactory</c> from an options instance.</summary>
        /// <param name="options">PostgreSQL People options.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddPeopleDbContextFactory(PostgresPeopleOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<PeopleDbContext, PostgresPeopleOptions>(options);

            return services;
        }

        /// <summary>Binds <see cref="PostgresPeopleStore" /> to <see cref="IPeopleStore" />.</summary>
        public IServiceCollection AddPostgresPeopleStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IPeopleStore, PostgresPeopleStore>();
            return services;
        }
    }
}