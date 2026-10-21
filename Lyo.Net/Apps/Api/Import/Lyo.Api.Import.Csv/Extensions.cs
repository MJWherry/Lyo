using Lyo.Csv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Import.Csv;

/// <summary>Registers the CSV import handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="CsvImportFormatHandler" />. Also registers <see cref="Lyo.Csv.Models.ICsvService" /> when the host has not.</summary>
        public IServiceCollection AddCsvImport()
        {
            services.AddCsvService();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IImportFormatHandler, CsvImportFormatHandler>());
            return services;
        }
    }
}