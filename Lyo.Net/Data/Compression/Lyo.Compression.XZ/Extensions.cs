using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.XZ;

/// <summary>DI extension methods that register the XZ <see cref="ICompressorFactory" />.</summary>
public static class XzCompressorRegistration
{
    /// <summary>Registers <see cref="XzCompressorFactory" /> as an additional <see cref="ICompressorFactory" />. Idempotent.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXzCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = XzCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, XzCompressorFactory>());
        return services;
    }
}
