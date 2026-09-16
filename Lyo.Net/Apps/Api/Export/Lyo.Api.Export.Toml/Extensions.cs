using Lyo.Api.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export.Toml;

/// <summary>Adds the TOML export format handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="TomlExportFormatHandler" /> as an <see cref="IExportFormatHandler" />. Needs <see cref="ITomlService" />.</summary>
        public IServiceCollection AddTomlExport()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IExportFormatHandler, TomlExportFormatHandler>());
            return services;
        }
    }
}
