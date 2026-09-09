using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>Response from GET <c>files/{id}/presigned-read</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PresignedReadResponse(string Url)
{
    /// <inheritdoc />
    public override string ToString() => $"PresignedReadResponse: Url={Url}";
}
