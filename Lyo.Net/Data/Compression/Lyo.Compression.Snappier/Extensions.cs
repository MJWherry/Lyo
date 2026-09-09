using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Snappier;

/// <summary>DI helpers that register the Snappier <see cref="ICompressorFactory" />.</summary>
public static class SnappierCompressorRegistration
{
    /// <summary>Adds <see cref="SnappierCompressorFactory" /> as another <see cref="ICompressorFactory" />. Safe to call more than once.</summary>
    /// <param name="services">Collection to add the factory to.</param>
    /// <returns>The same collection so calls can be chained.</returns>
    public static IServiceCollection AddSnappierCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = SnappierCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, SnappierCompressorFactory>());
        return services;
    }
}