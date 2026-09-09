using Lyo.Api.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export.Csv;

/// <summary>Adds the CSV export format handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="CsvExportFormatHandler" /> as an <see cref="IExportFormatHandler" />. Needs <see cref="Lyo.Csv.Models.ICsvService" />.</summary>
        public IServiceCollection AddCsvExport()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IExportFormatHandler, CsvExportFormatHandler>());
            return services;
        }
    }
}