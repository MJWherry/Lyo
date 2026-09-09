using Lyo.Reporting.Models.Composition;
using Lyo.Seed;

namespace Lyo.TestGateway.Seeds;

/// <summary>
/// Posts the designer sample compositions to TestApi via <c>POST Reporting/Definition/Bulk</c>.
/// Count is ignored — the catalog is a fixed set of templates with example parameter JSON copied onto definition values.
/// </summary>
public sealed class ReportingApiSeedContributor : SeedContributor
{
    /// <inheritdoc />
    public override string Name => "Reporting API";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Api;

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
        => graph.Entity(ReportDesignTemplates.Samples.Select(s => s.ToDefinitionReq()));
}
