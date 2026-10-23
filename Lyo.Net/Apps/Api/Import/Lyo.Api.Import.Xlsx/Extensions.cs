using Lyo.Xlsx;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Import.Xlsx;

/// <summary>Registers the XLSX import handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="XlsxImportFormatHandler" />. Also registers <see cref="Lyo.Xlsx.Models.IXlsxService" /> when the host has not.</summary>
        public IServiceCollection AddXlsxImport()
        {
            services.AddXlsxService();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IImportFormatHandler, XlsxImportFormatHandler>());
            return services;
        }
    }
}