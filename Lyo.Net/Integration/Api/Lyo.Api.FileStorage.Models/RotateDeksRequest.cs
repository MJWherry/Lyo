using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>files/rotate-deks</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RotateDeksRequest(IReadOnlyCollection<Guid> FileIds, string? TargetKeyId = null, string? TargetKeyVersion = null, int BatchSize = 100)
{
    /// <inheritdoc />
    public override string ToString()
        => $"RotateDeksRequest: FileCount={FileIds.Count}, TargetKeyId={TargetKeyId ?? "(none)"}, TargetKeyVersion={TargetKeyVersion ?? "(none)"}, BatchSize={BatchSize}";
}
