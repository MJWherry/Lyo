using Lyo.Api.ApiEndpoint;
using Lyo.Api.Services.Export;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api.Export;

/// <summary>Adds the export API feature and <see cref="IExportService{TContext}" />.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="ExportEndpointContributor" /> and a scoped <see cref="IExportService{TContext}" /> used by export endpoints.</summary>
        public IServiceCollection AddLyoApiExport<TContext>()
            where TContext : DbContext
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IApiEndpointContributor, ExportEndpointContributor>());
            services.TryAddScoped<IExportService<TContext>, ExportService<TContext>>();
            return services;
        }
    }
}