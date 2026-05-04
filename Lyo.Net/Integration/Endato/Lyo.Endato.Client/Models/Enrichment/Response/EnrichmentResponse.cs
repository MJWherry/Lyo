using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EnrichmentResponse(
    Person? Person,
    int IdentityScore,
    int TotalRequestExecutionTimeMs,
    Guid RequestId,
    string RequestType,
    DateTime RequestTime,
    bool IsError
    /*"error": {
        "inputErrors": [],
        "warnings": []
    }*/
)
{
    public override string ToString()
        => $"EnrichmentResponse: Person={(Person == null ? "null" : "present")}, IdentityScore={IdentityScore}, " +
           $"RequestId={RequestId}, Type='{RequestType}', TimeMs={TotalRequestExecutionTimeMs}, " +
           $"RequestTime={RequestTime:O}, Error={IsError}";
}