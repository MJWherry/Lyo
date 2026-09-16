using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileTags.FFmpeg;

/// <summary>DI helpers that register the audio and video tag provider.</summary>
public static class Extensions
{
    /// <param name="services">Collection to add the provider to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="FFmpegFileTagProvider" /> and <see cref="IFileTagService" />. A host <see cref="FFmpegOptions" /> already in the collection is left as-is.</summary>
        public IServiceCollection AddFFmpegFileTags() => services.AddFFmpegFileTags(new());

        /// <summary>Registers the provider with <paramref name="options" /> when the host has not registered <see cref="FFmpegOptions" /> yet.</summary>
        public IServiceCollection AddFFmpegFileTags(FFmpegOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(options);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileTagReader, FFmpegFileTagProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileTagWriter, FFmpegFileTagProvider>());
            return services.AddFileTagService();
        }
    }
}
