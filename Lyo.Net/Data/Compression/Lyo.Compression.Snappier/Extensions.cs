using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Snappier;

/// <summary>DI extension methods that register the Snappier <see cref="ICompressorFactory" />.</summary>
public static class SnappierCompressorRegistration
{
    /// <summary>Registers <see cref="SnappierCompressorFactory" /> as an additional <see cref="ICompressorFactory" />. Idempotent.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnappierCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = SnappierCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, SnappierCompressorFactory>());
        return services;
    }
}
