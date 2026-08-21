using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>multipart/complete</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CompleteMultipartRequest(Guid SessionId, IReadOnlyList<CompletedPart> Parts)
{
    /// <inheritdoc />
    public override string ToString() => $"CompleteMultipartRequest: SessionId={SessionId}, PartCount={Parts.Count}";
}
