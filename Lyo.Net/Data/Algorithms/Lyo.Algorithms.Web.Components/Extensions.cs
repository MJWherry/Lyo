using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Algorithms.Web.Components;

/// <summary>DI helpers for the algorithm workbench.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers an empty <see cref="AlgorithmExampleContext" />.</summary>
        public IServiceCollection AddAlgorithmWorkbench() => services.AddAlgorithmWorkbench(static _ => { });

        /// <summary>Registers the sample context the workbench merges into a new bag.</summary>
        public IServiceCollection AddAlgorithmWorkbench(Action<AlgorithmExampleContext> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var context = new AlgorithmExampleContext();
            configure(context);
            services.TryAddSingleton(context);
            return services;
        }
    }
}
