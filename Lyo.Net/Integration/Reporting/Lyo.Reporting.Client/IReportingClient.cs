namespace Lyo.Reporting.Client;

/// <summary>Typed HTTP client for the Lyo Reporting API.</summary>
public interface IReportingClient
{
    ReportDefinitionClient Definitions { get; }

    ReportDefinitionParameterClient DefinitionParameters { get; }

    ReportGenerationClient Generations { get; }
}