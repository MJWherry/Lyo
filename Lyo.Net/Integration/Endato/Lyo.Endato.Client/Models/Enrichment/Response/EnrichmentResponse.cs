using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

/// <summary>Endato Contact Enrichment top-level response envelope.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EnrichmentResponse(
    Person? Person,
    int IdentityScore,
    int TotalRequestExecutionTimeMs,
    Guid RequestId,
    string RequestType,
    DateTime RequestTime,
    bool IsError,
    EndatoErrorDetails? Error = null)
{
    public override string ToString()
        => $"EnrichmentResponse: Person={(Person == null ? "null" : Person.ToString())}, IdentityScore={IdentityScore}, RequestId={RequestId}, Type='{RequestType}', TimeMs={TotalRequestExecutionTimeMs}, RequestTime={RequestTime:O}, Error={IsError}";
}