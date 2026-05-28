using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.BZip2;

/// <summary>DI extension methods that register the BZip2 <see cref="ICompressorFactory" />.</summary>
public static class BZip2CompressorRegistration
{
    /// <summary>Registers <see cref="BZip2CompressorFactory" /> as an additional <see cref="ICompressorFactory" />. Idempotent.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBZip2Compressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = BZip2CompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, BZip2CompressorFactory>());
        return services;
    }
}
