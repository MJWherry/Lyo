using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.LZMA;

/// <summary>DI extension methods that register the LZMA <see cref="ICompressorFactory" />.</summary>
public static class LzmaCompressorRegistration
{
    /// <summary>Registers <see cref="LzmaCompressorFactory" /> as an additional <see cref="ICompressorFactory" />. Idempotent.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLzmaCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = LzmaCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, LzmaCompressorFactory>());
        return services;
    }
}