using System.Diagnostics;

namespace Lyo.Endato.Client.Models;

/// <summary>Pagination metadata for paged Endato search responses.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Pagination(int CurrentPageNumber, int ResultsPerPage, int TotalPages, int TotalResults)
{
    public override string ToString() => $"Pagination: {CurrentPageNumber}/{TotalPages}, {ResultsPerPage} per page, {TotalResults} total";
}
