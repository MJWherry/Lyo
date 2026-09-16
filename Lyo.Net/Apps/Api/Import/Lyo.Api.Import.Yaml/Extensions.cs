using Lyo.Api.Import;
using Lyo.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Import.Yaml;

/// <summary>Registers the YAML import handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="YamlImportFormatHandler" />. Also registers <see cref="IYamlService" /> when the host has not.</summary>
        public IServiceCollection AddYamlImport()
        {
            services.TryAddSingleton<IYamlService, YamlService>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IImportFormatHandler, YamlImportFormatHandler>());
            return services;
        }
    }
}
