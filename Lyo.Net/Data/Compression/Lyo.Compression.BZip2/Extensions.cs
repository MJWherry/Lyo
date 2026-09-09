using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.BZip2;

/// <summary>DI helpers that register the BZip2 <see cref="ICompressorFactory" />.</summary>
public static class BZip2CompressorRegistration
{
    /// <summary>Adds <see cref="BZip2CompressorFactory" /> as another <see cref="ICompressorFactory" />. Safe to call more than once.</summary>
    /// <param name="services">Collection to add the factory to.</param>
    /// <returns>The same collection so calls can be chained.</returns>
    public static IServiceCollection AddBZip2Compressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = BZip2CompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, BZip2CompressorFactory>());
        return services;
    }
}