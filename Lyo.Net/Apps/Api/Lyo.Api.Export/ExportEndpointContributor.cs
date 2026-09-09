using Lyo.Api.ApiEndpoint;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Export;

/// <summary>Adds export routes on typed and dynamic CRUD builders.</summary>
public sealed class ExportEndpointContributor : IApiEndpointContributor
{
    public ApiFeature Feature => ExportApiFeature.Instance;

    public void ConfigureTypedCrud(IApiEndpointCrudContributorContext context) => context.EnableExport();

    public void RegisterDynamicRoutes<TContext>(IDynamicApiEndpointContributorContext<TContext> context)
        where TContext : DbContext
        => context.RegisterExportRoute();
}