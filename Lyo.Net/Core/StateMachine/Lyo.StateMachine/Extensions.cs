using Lyo.Exceptions;
using Lyo.StateMachine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.StateMachine;

/// <summary>DI registration for state machine definitions and observers.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a <see cref="StateMachineDefinition{TState,TTrigger,TContext}" /> singleton. The configuration is validated here, so a bad definition fails at startup. Every
        /// <see cref="IStateMachineObserver{TState,TTrigger}" /> registered in DI is attached when the definition is first resolved.
        /// </summary>
        /// <exception cref="StateMachineConfigurationException">The configuration is invalid.</exception>
        public IServiceCollection AddStateMachine<TState, TTrigger, TContext>(string name, TState initialState, Action<StateMachineBuilder<TState, TTrigger, TContext>> configure)
            where TState : notnull where TTrigger : notnull
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var builder = new StateMachineBuilder<TState, TTrigger, TContext>(name, initialState);
            configure(builder);
            builder.Build();
            services.TryAddSingleton(sp => {
                foreach (var observer in sp.GetServices<IStateMachineObserver<TState, TTrigger>>())
                    builder.AddObserver(observer);

                return builder.Build();
            });

            return services;
        }

        /// <summary>Registers an observer for every machine with these state and trigger types. Call before the definition is first resolved.</summary>
        public IServiceCollection AddStateMachineObserver<TState, TTrigger, TObserver>()
            where TState : notnull where TTrigger : notnull where TObserver : class, IStateMachineObserver<TState, TTrigger>
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStateMachineObserver<TState, TTrigger>, TObserver>());
            return services;
        }

        /// <summary>Attaches <see cref="MetricsStateMachineObserver{TState,TTrigger}" /> to machines with these types. Requires an <c>IMetrics</c> registration.</summary>
        public IServiceCollection AddStateMachineMetrics<TState, TTrigger>()
            where TState : notnull where TTrigger : notnull
            => services.AddStateMachineObserver<TState, TTrigger, MetricsStateMachineObserver<TState, TTrigger>>();

        /// <summary>Attaches <see cref="LoggingStateMachineObserver{TState,TTrigger}" /> to machines with these types. Requires logging (<c>AddLogging</c>).</summary>
        public IServiceCollection AddStateMachineLogging<TState, TTrigger>()
            where TState : notnull where TTrigger : notnull
            => services.AddStateMachineObserver<TState, TTrigger, LoggingStateMachineObserver<TState, TTrigger>>();
    }
}