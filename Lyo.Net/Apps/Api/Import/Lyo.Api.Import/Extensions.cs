using Lyo.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Import;

/// <summary>Registers JSON import and <see cref="IImportService" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="JsonImportFormatHandler" /> and <see cref="ImportService" />. YAML, XML, TOML, CSV, and XLSX handlers are added by their addon packages. Options are
        /// validated at registration.
        /// </summary>
        public IServiceCollection AddLyoApiImport(ImportOptions? options = null)
        {
            var importOptions = options ?? new ImportOptions();
            importOptions.Validate();
            services.TryAddSingleton<IJsonService>(JsonService.Default);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IImportFormatHandler, JsonImportFormatHandler>());
            services.TryAddSingleton(importOptions);
            services.TryAddSingleton<IImportService>(provider => new ImportService(
                provider.GetServices<IImportFormatHandler>(), provider.GetRequiredService<ImportOptions>(), provider.GetService<IImportCommitter>()));

            return services;
        }
    }
}