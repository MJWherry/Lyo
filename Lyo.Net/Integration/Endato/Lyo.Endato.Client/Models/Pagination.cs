using System.Diagnostics;

namespace Lyo.Endato.Client.Models;

/// <summary>Paged Endato search responses pagination metadata.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Pagination(int CurrentPageNumber, int ResultsPerPage, int TotalPages, int TotalResults)
{
    public override string ToString() => $"Pagination: {CurrentPageNumber}/{TotalPages}, {ResultsPerPage} per page, {TotalResults} total";
}