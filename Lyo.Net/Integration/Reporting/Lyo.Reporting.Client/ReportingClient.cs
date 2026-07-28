using Lyo.Api.Client;
using Lyo.Exceptions;

namespace Lyo.Reporting.Client;

/// <summary>Typed sub-client for the Lyo Reporting API.</summary>
public sealed class ReportingClient : IReportingClient
{
    public ReportingClient(IApiClient apiClient, ReportingClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        var routePrefix = options?.RoutePrefix;
        Definitions = new(apiClient, routePrefix);
        DefinitionParameters = new(apiClient, routePrefix);
        Generations = new(apiClient, routePrefix);
    }

    public ReportDefinitionClient Definitions { get; }

    public ReportDefinitionParameterClient DefinitionParameters { get; }

    public ReportGenerationClient Generations { get; }
}