using Lyo.Api.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export.Csv;

/// <summary>Registers the CSV export format handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="CsvExportFormatHandler" /> as an <see cref="IExportFormatHandler" />. Requires <see cref="Lyo.Csv.Models.ICsvService" />.</summary>
        public IServiceCollection AddCsvExport()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IExportFormatHandler, CsvExportFormatHandler>());
            return services;
        }
    }
}
