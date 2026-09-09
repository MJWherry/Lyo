using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint;

/// <summary>Registers optional API features (export, and others) with typed and dynamic CRUD endpoint builders.</summary>
public interface IApiEndpointContributor
{
    ApiFeature Feature { get; }

    void ConfigureTypedCrud(IApiEndpointCrudContributorContext context);

    void RegisterDynamicRoutes<TContext>(IDynamicApiEndpointContributorContext<TContext> context)
        where TContext : DbContext;
}

/// <summary>Context used to configure typed CRUD endpoints from an <see cref="IApiEndpointContributor" />.</summary>
public interface IApiEndpointCrudContributorContext
{
    void EnableExport(EndpointAuth? auth = null);
}

/// <summary>Context used to register dynamic CRUD routes from an <see cref="IApiEndpointContributor" />.</summary>
public interface IDynamicApiEndpointContributorContext<TContext>
    where TContext : DbContext
{
    void RegisterExportRoute();
}