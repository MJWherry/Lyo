using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Lz4;

/// <summary>DI helpers that register the LZ4 <see cref="ICompressorFactory" /> so <c>CompressionService</c> can resolve <see cref="Lz4CompressionAlgorithm" />.</summary>
public static class Lz4CompressorRegistration
{
    /// <summary>Adds <see cref="Lz4CompressorFactory" /> as another <see cref="ICompressorFactory" />. Safe to call more than once.</summary>
    /// <param name="services">Collection to add the factory to.</param>
    /// <returns>The same collection so calls can be chained.</returns>
    public static IServiceCollection AddLz4Compressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = Lz4CompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, Lz4CompressorFactory>());
        return services;
    }
}