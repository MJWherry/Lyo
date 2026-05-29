namespace Lyo.Api.ApiEndpoint;

internal sealed class ApiEndpointCrudContributorContext(EndpointAuth? defaultExportAuth, Action<EndpointAuth?> enableExport) : IApiEndpointCrudContributorContext
{
    public void EnableExport(EndpointAuth? auth = null) => enableExport(auth ?? defaultExportAuth);
}