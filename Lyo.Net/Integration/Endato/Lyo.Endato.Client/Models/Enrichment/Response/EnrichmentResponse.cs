namespace Lyo.Endato.Client.Models.Enrichment.Response;

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
);