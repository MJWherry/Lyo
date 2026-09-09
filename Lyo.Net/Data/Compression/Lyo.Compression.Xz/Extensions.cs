using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Xz;

/// <summary>DI helpers that register the XZ <see cref="ICompressorFactory" />.</summary>
public static class XzCompressorRegistration
{
    /// <summary>Adds <see cref="XzCompressorFactory" /> as another <see cref="ICompressorFactory" />. Safe to call more than once.</summary>
    /// <param name="services">Collection to add the factory to.</param>
    /// <returns>The same collection so calls can be chained.</returns>
    public static IServiceCollection AddXzCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = XzCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, XzCompressorFactory>());
        return services;
    }
}