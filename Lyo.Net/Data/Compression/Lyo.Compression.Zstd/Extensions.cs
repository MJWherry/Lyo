using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Zstd;

/// <summary>DI helpers that register the Zstd <see cref="ICompressorFactory" />.</summary>
public static class ZstdCompressorRegistration
{
    /// <summary>Adds <see cref="ZstdCompressorFactory" /> as another <see cref="ICompressorFactory" />. Safe to call more than once.</summary>
    /// <param name="services">Collection to add the factory to.</param>
    /// <returns>The same collection so calls can be chained.</returns>
    public static IServiceCollection AddZstdCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = ZstdCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, ZstdCompressorFactory>());
        return services;
    }
}