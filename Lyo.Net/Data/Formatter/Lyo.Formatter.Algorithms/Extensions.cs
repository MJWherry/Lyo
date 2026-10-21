using Lyo.Algorithms;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Formatter.Algorithms;

/// <summary>Registers <see cref="AlgorithmFormatterExpressionEvaluator" />. Call <c>AddFormatterService</c> as well. Job and Api do not.</summary>
public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Uses <see cref="IAlgorithmRegistry" /> for <c>{Algo("key")}</c> and <c>{score("key")}</c>. Register <c>AddAlgorithms</c> first. <c>AddFormatterService</c> stays free of
        /// an Algorithms reference until this is called.
        /// </summary>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddFormatterAlgorithms()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IFormatterExpressionEvaluator>(provider => new AlgorithmFormatterExpressionEvaluator(provider.GetRequiredService<IAlgorithmRegistry>()));
            return services;
        }
    }
}