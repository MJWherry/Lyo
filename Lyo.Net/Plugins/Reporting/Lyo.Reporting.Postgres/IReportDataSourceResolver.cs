using Lyo.Query.Models.Common.Request;

namespace Lyo.Reporting.Postgres;

/// <summary>Host adapter that runs a root <see cref="QueryReq" /> for report parameter Options. Missing registration fails closed when a Query dataset is used.</summary>
public interface IReportRootQuery
{
    /// <summary>Executes <paramref name="request" /> and returns projected rows.</summary>
    Task<IReadOnlyList<object?>> QueryAsync(QueryReq request, CancellationToken ct = default);
}

/// <summary>Executes Query/Sproc parameter Options after merge and fills FromParameter and Query/Sproc tables before render.</summary>
public interface IReportDataSourceResolver
{
    /// <summary>
    /// Materializes dataset parameters and copies row JSON into matching tables. Also runs Query/Sproc Options stored on the table itself. Returns updated composition JSON.
    /// </summary>
    Task<string> ApplyAsync(
        string reportDataJson,
        IReadOnlyList<ReportParameterOptionsRef> definitionParameters,
        IList<Lyo.Reporting.Models.Request.ReportGenerationParameterReq> mergedParameters,
        CancellationToken ct = default);
}

/// <summary>Definition-parameter Options needed to resolve Query/Sproc datasets (no EF type).</summary>
public readonly record struct ReportParameterOptionsRef(string Key, string Type, string? Options);
