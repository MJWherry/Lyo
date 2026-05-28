using Lyo.Api.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export.Xlsx;

/// <summary>Registers the XLSX export format handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="XlsxExportFormatHandler" /> as an <see cref="IExportFormatHandler" />. Requires <see cref="Lyo.Xlsx.Models.IXlsxService" />.</summary>
        public IServiceCollection AddXlsxExport()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IExportFormatHandler, XlsxExportFormatHandler>());
            return services;
        }
    }
}
