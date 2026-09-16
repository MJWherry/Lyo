using Lyo.Api.Import;
using Lyo.Toml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Import.Toml;

/// <summary>Registers the TOML import handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="TomlImportFormatHandler" />. Also registers <see cref="ITomlService" /> when the host has not.</summary>
        public IServiceCollection AddTomlImport()
        {
            services.TryAddSingleton<ITomlService, TomlService>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IImportFormatHandler, TomlImportFormatHandler>());
            return services;
        }
    }
}
