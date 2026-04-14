namespace Lyo.Endato.Client.Models;

public sealed record Pagination(int CurrentPageNumber, int ResultsPerPage, int TotalPages, int TotalResults);