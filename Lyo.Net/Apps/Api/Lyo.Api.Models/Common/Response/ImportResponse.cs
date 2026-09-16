namespace Lyo.Api.Models.Common.Response;

/// <summary>Import outcome. <see cref="IsSuccess" /> stays true when individual rows fail. Document failures return no rows.</summary>
public sealed class ImportResponse
{
    /// <summary>False for an empty file, a bad shape, an unknown format, a duplicate map, or a cap.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Document-level message. Null on success.</summary>
    public string? Message { get; set; }

    /// <summary>Good rows as a JSON array. Property names match the import binding.</summary>
    public string RowsJson { get; set; } = "[]";

    /// <summary>Rows that were left out.</summary>
    public List<ImportIssue> Issues { get; set; } = [];
}
