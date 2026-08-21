using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>One completed multipart part.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CompletedPart(int PartNumber, string ETagOrBlockId)
{
    /// <inheritdoc />
    public override string ToString() => $"CompletedPart: PartNumber={PartNumber}, ETagOrBlockId={ETagOrBlockId}";
}
