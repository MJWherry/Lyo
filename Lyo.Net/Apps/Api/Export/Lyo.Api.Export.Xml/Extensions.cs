using Lyo.Api.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export.Xml;

/// <summary>Adds the XML export format handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="XmlExportFormatHandler" /> as an <see cref="IExportFormatHandler" />. Needs <see cref="IXmlService" />.</summary>
        public IServiceCollection AddXmlExport()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IExportFormatHandler, XmlExportFormatHandler>());
            return services;
        }
    }
}
