using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileTags;

/// <summary>DI helpers for <see cref="IFileTagService" />. Register readers and writers first so the dispatcher sees them.</summary>
public static class Extensions
{
    /// <param name="services">Collection that already has the tag providers.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="FileTagService" /> as <see cref="IFileTagService" /> when the host has not registered one.</summary>
        public IServiceCollection AddFileTagService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IFileTagService, FileTagService>();
            return services;
        }
    }
}
