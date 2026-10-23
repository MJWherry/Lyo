using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.StateMachine.Models;
using Lyo.StateMachine.Postgres.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.StateMachine.Postgres;

/// <summary>Helpers that register PostgreSQL state machine storage.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the <see cref="StateMachineDbContext" /> factory from a configure delegate. Follow with <c>AddPostgresStateStore</c> per state type.</summary>
        public IServiceCollection AddStateMachinePostgres(Action<PostgresStateMachineOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresStateMachineOptions();
            configure(options);
            return services.AddStateMachinePostgres(options);
        }

        /// <summary>Registers the <see cref="StateMachineDbContext" /> factory from configuration. A missing section leaves defaults, so the connection string check fails.</summary>
        public IServiceCollection AddStateMachinePostgresFromConfiguration(IConfiguration configuration, string configSectionName = PostgresStateMachineOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresStateMachineOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddStateMachinePostgres(options);
        }

        /// <summary>Registers the <see cref="StateMachineDbContext" /> factory (schema <c>state_machine</c>) and optional auto-migrations.</summary>
        public IServiceCollection AddStateMachinePostgres(PostgresStateMachineOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<StateMachineDbContext, PostgresStateMachineOptions>(options);
            return services;
        }

        /// <summary>
        /// Registers <see cref="IStateStore{TState}" /> and <see cref="IStateInstanceQuery{TState}" /> for an enum state type, stored by member name. Requires
        /// <c>AddStateMachinePostgres</c>.
        /// </summary>
        public IServiceCollection AddPostgresStateStore<TState>()
            where TState : struct, Enum
            => services.AddPostgresStateStore(EnumStateKeyConverter<TState>.Instance);

        /// <summary>
        /// Registers <see cref="IStateStore{TState}" /> and <see cref="IStateInstanceQuery{TState}" /> (one shared <see cref="PostgresStateStore{TState}" />) for any state type
        /// using a custom key converter. Requires <c>AddStateMachinePostgres</c>.
        /// </summary>
        public IServiceCollection AddPostgresStateStore<TState>(IStateKeyConverter<TState> converter)
            where TState : notnull
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(converter);
            services.TryAddSingleton(converter);
            services.TryAddSingleton<PostgresStateStore<TState>>();
            services.TryAddSingleton<IStateStore<TState>>(sp => sp.GetRequiredService<PostgresStateStore<TState>>());
            services.TryAddSingleton<IStateInstanceQuery<TState>>(sp => sp.GetRequiredService<PostgresStateStore<TState>>());
            return services;
        }

        /// <summary>Registers <see cref="IStateMachineDefinitionStore" /> backed by the <c>definition</c> table. Requires <c>AddStateMachinePostgres</c>.</summary>
        public IServiceCollection AddPostgresStateMachineDefinitionStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IStateMachineDefinitionStore, PostgresStateMachineDefinitionStore>();
            return services;
        }
    }
}