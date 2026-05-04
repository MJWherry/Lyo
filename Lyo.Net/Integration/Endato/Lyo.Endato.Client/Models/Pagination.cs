using System.Diagnostics;

namespace Lyo.Endato.Client.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Pagination(int CurrentPageNumber, int ResultsPerPage, int TotalPages, int TotalResults)
{
    public override string ToString()
        => $"{CurrentPageNumber}/{TotalPages}, {ResultsPerPage} per page, {TotalResults} total";
}