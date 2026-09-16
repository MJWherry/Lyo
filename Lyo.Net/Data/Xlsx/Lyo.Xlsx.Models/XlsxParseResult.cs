using Lyo.Result;

namespace Lyo.Xlsx.Models;

/// <summary>Typed sheet parse that keeps good rows when other rows have a bad cell. A missing sheet or a missing required header fails the document.</summary>
/// <typeparam name="T">Row type.</typeparam>
public sealed record XlsxParseResult<T> : Result<IReadOnlyList<T>>
{
    /// <summary>Row problems. Empty or null when every data row bound.</summary>
    public IReadOnlyList<XlsxParseError>? ParseErrors { get; init; }

    private XlsxParseResult(bool isSuccess, List<T>? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors)
    {
    }

    /// <summary>Partial or full success. <paramref name="parseErrors" /> may be set while success stays true.</summary>
    public static XlsxParseResult<T> FromSuccess(List<T> rows, IReadOnlyList<XlsxParseError>? parseErrors = null)
        => new(true, rows) { ParseErrors = parseErrors };

    /// <summary>Document failure. No rows are returned.</summary>
    public static XlsxParseResult<T> FromError(string message, string code)
        => new(false, null, [new(message, code)]);
}
