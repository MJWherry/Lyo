using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileTags.Images;

/// <summary>DI helpers that register the picture tag provider.</summary>
public static class Extensions
{
    /// <param name="services">Collection to add the provider to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="ImageFileTagProvider" /> and <see cref="IFileTagService" />.</summary>
        public IServiceCollection AddImageFileTags()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileTagReader, ImageFileTagProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileTagWriter, ImageFileTagProvider>());
            return services.AddFileTagService();
        }
    }
}
