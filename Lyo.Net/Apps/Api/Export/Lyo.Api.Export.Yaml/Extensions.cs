using Lyo.Api.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export.Yaml;

/// <summary>Adds the YAML export format handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="YamlExportFormatHandler" /> as an <see cref="IExportFormatHandler" />. Needs <see cref="IYamlService" />.</summary>
        public IServiceCollection AddYamlExport()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IExportFormatHandler, YamlExportFormatHandler>());
            return services;
        }
    }
}
