using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Zstd;

/// <summary>DI extension methods that register the Zstd <see cref="ICompressorFactory" />.</summary>
public static class ZstdCompressorRegistration
{
    /// <summary>Registers <see cref="ZstdCompressorFactory" /> as an additional <see cref="ICompressorFactory" />. Idempotent.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddZstdCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = ZstdCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, ZstdCompressorFactory>());
        return services;
    }
}