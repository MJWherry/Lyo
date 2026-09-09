using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Compression.Lzma;

/// <summary>DI helpers that register the LZMA <see cref="ICompressorFactory" />.</summary>
public static class LzmaCompressorRegistration
{
    /// <summary>Adds <see cref="LzmaCompressorFactory" /> as another <see cref="ICompressorFactory" />. Safe to call more than once.</summary>
    /// <param name="services">Collection to add the factory to.</param>
    /// <returns>The same collection so calls can be chained.</returns>
    public static IServiceCollection AddLzmaCompressor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _ = LzmaCompressionAlgorithm.Instance;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, LzmaCompressorFactory>());
        return services;
    }
}