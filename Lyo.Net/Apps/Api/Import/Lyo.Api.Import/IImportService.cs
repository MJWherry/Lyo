using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;

namespace Lyo.Api.Import;

/// <summary>Validates an import request and dispatches to a format handler. Does not write a store.</summary>
public interface IImportService
{
    /// <summary>Imports <paramref name="request" /> as <typeparamref name="T" />. Duplicate targets, unknown formats, and caps fail the document.</summary>
    ImportResult<T> Import<T>(ImportRequest request)
        where T : new();

    /// <summary>CSV view of <paramref name="issues" /> with columns Index, Field, Message, Raw.</summary>
    string WriteErrorReport(IReadOnlyList<ImportIssue> issues);
}
