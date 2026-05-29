using Lyo.Api.ApiEndpoint;
using Lyo.Api.Services.Export;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export;

/// <summary>Registers the export API feature and <see cref="IExportService{TContext}" />.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="ExportEndpointContributor" /> and scoped <see cref="IExportService{TContext}" /> for export endpoints.</summary>
        public IServiceCollection AddLyoApiExport<TContext>()
            where TContext : DbContext
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IApiEndpointContributor, ExportEndpointContributor>());
            services.TryAddScoped<IExportService<TContext>, ExportService<TContext>>();
            return services;
        }
    }
}