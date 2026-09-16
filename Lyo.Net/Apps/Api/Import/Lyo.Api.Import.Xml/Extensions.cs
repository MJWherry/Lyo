using Lyo.Api.Import;
using Lyo.Xml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Import.Xml;

/// <summary>Registers the XML import handler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="XmlImportFormatHandler" />. Also registers <see cref="IXmlService" /> when the host has not.</summary>
        public IServiceCollection AddXmlImport()
        {
            services.TryAddSingleton<IXmlService, XmlService>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IImportFormatHandler, XmlImportFormatHandler>());
            return services;
        }
    }
}
