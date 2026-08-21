using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>files/migrate-deks</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MigrateDeksRequest(
    string SourceKeyId,
    string? SourceKeyVersion = null,
    string? TargetKeyId = null,
    string? TargetKeyVersion = null,
    int BatchSize = 100)
{
    /// <inheritdoc />
    public override string ToString()
        => $"MigrateDeksRequest: SourceKeyId={SourceKeyId}, SourceKeyVersion={SourceKeyVersion ?? "(none)"}, TargetKeyId={TargetKeyId ?? "(none)"}, BatchSize={BatchSize}";
}
