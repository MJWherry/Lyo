using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Lz4;

/// <summary>DI extension methods that register the LZ4 <see cref="ICompressorFactory" /> so <c>CompressionService</c> can resolve <see cref="Lz4CompressionAlgorithm" />.</summary>
public static class Lz4CompressorRegistration
{
    /// <summary>Registers <see cref="Lz4CompressorFactory" /> as an additional <see cref="ICompressorFactory" />. Idempotent.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLz4Compressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = Lz4CompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, Lz4CompressorFactory>());
        return services;
    }
}