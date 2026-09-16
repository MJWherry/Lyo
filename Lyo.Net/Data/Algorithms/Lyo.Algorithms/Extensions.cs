using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Algorithms;

/// <summary>DI helpers for the algorithm registry, expression compiler, and in-memory catalog.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the compiler, frozen startup store, runtime store, and registry with default options.</summary>
        public IServiceCollection AddAlgorithms()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddAlgorithms(new AlgorithmOptions());
        }

        /// <summary>Registers algorithms using <paramref name="configure" />. Startup expressions are compiled before this returns.</summary>
        public IServiceCollection AddAlgorithms(Action<AlgorithmOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new AlgorithmOptions();
            configure(options);
            return services.AddAlgorithms(options);
        }

        /// <summary>Binds <see cref="AlgorithmOptions.SectionName" /> (or <paramref name="sectionName" />) and registers algorithms.</summary>
        public IServiceCollection AddAlgorithmsFromConfiguration(IConfiguration configuration, string sectionName = AlgorithmOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new AlgorithmOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddAlgorithms(options);
        }

        /// <summary>Registers algorithms from <paramref name="options" />. Invalid startup expressions throw <see cref="AlgorithmException" />.</summary>
        public IServiceCollection AddAlgorithms(AlgorithmOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            var compiler = new ExpressionCompiler();
            foreach (var expression in options.Expressions) {
                var compiled = compiler.Compile<AlgorithmContextBag, object>(expression.Expression);
                if (!compiled.IsSuccess || compiled.Data is null)
                    AlgorithmHelpers.Throw(compiled.Errors?[0].Message ?? $"Expression '{expression.Key}' failed to compile.");
                services.AddSingleton<IAlgorithmRegistration>(new AlgorithmRegistration<AlgorithmContextBag, object>(
                    expression.Key, new KeyedAlgorithm<AlgorithmContextBag, object>(expression.Key, compiled.Data)));
            }

            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.TryAddSingleton<IExpressionCompiler>(compiler);
            services.TryAddSingleton<StartupAlgorithmStore>(sp => new StartupAlgorithmStore(sp.GetServices<IAlgorithmRegistration>()));
            services.TryAddSingleton<IAlgorithmStartupStore>(sp => sp.GetRequiredService<StartupAlgorithmStore>());
            services.TryAddSingleton<RuntimeAlgorithmStore>(sp => new RuntimeAlgorithmStore(
                sp.GetRequiredService<IExpressionCompiler>(), sp.GetRequiredService<IAlgorithmStartupStore>(), sp.GetRequiredService<AlgorithmOptions>(),
                sp.GetService<IAlgorithmDefinitionStore>()));
            services.TryAddSingleton<IAlgorithmRuntimeStore>(sp => sp.GetRequiredService<RuntimeAlgorithmStore>());
            services.TryAddSingleton<IAlgorithmRegistry, AlgorithmRegistry>();
            services.TryAddSingleton<IAlgorithmCatalog>(sp => {
                var store = sp.GetService<IAlgorithmDefinitionStore>();
                if (store is null)
                    AlgorithmHelpers.Throw("No IAlgorithmDefinitionStore is registered. Call AddMemoryAlgorithmStore, AddPostgresAlgorithmStore, or AddSqliteAlgorithmStore.");
                return new AlgorithmCatalog(
                    store,
                    sp.GetRequiredService<IExpressionCompiler>(),
                    sp.GetRequiredService<AlgorithmOptions>(),
                    sp.GetRequiredService<IAlgorithmStartupStore>(),
                    sp.GetRequiredService<RuntimeAlgorithmStore>());
            });
            return services;
        }

        /// <summary>Registers an in-memory <see cref="IAlgorithmDefinitionStore" />. Call before the registry is resolved.</summary>
        public IServiceCollection AddMemoryAlgorithmStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<MemoryAlgorithmDefinitionStore>();
            services.TryAddSingleton<IAlgorithmDefinitionStore>(sp => sp.GetRequiredService<MemoryAlgorithmDefinitionStore>());
            return services;
        }

        /// <summary>Freezes a named C# algorithm into the startup store. Duplicate key and type pairs fail when the registry is created.</summary>
        public IServiceCollection AddNamedAlgorithm<TImpl, TContext, TResult>(string key) where TImpl : class, INamedAlgorithm<TContext, TResult>
        {
            ArgumentHelpers.ThrowIfNull(services);
            AlgorithmKeys.ThrowIfInvalidKey(key);
            services.TryAddSingleton<TImpl>();
            services.AddSingleton<IAlgorithmRegistration>(sp => new AlgorithmRegistration<TContext, TResult>(key, sp.GetRequiredService<TImpl>()));
            return services;
        }

        /// <summary>Compiles <paramref name="expression" /> now and freezes it into the startup store.</summary>
        public IServiceCollection AddExpressionAlgorithm<TContext, TResult>(string key, string expression)
        {
            ArgumentHelpers.ThrowIfNull(services);
            AlgorithmKeys.ThrowIfInvalidKey(key);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(expression);
            var compiled = new ExpressionCompiler().Compile<TContext, TResult>(expression);
            if (!compiled.IsSuccess || compiled.Data is null)
                AlgorithmHelpers.Throw(compiled.Errors?[0].Message ?? $"Expression '{key}' failed to compile.");
            services.AddSingleton<IAlgorithmRegistration>(new AlgorithmRegistration<TContext, TResult>(key, new KeyedAlgorithm<TContext, TResult>(key, compiled.Data)));
            return services;
        }
    }
}
