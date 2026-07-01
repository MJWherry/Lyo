using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Top-level response envelope for Endato Person Search.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PersonQueryResponse(
    IReadOnlyList<Person> Persons,
    int TotalRequestExecutionTimeMs,
    Guid RequestId,
    string RequestType,
    DateTime RequestTime,
    bool IsError,
    Pagination? Pagination = null,
    Counts? Counts = null,
    [property: JsonPropertyName("searchCriteria")]
    IReadOnlyList<CriteriaType>? SearchCriteria = null,
    EndatoErrorDetails? Error = null)
{
    public override string ToString()
        => $"PersonQueryResponse: Persons={Persons.Count}, RequestId={RequestId}, Type='{RequestType}', TimeMs={TotalRequestExecutionTimeMs}, RequestTime={RequestTime:O}, Error={IsError}, Pagination={Pagination}, Counts={Counts}";
}