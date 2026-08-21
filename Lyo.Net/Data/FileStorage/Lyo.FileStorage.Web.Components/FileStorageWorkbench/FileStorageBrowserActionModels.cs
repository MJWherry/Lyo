using System.Diagnostics;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

/// <summary>Result of the move or copy path-prefix dialog.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorePathPrefixDialogResult(string? PathPrefix)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStorePathPrefixDialogResult: PathPrefix={PathPrefix ?? "(none)"}";
}

/// <summary>Result of the rename dialog (metadata display name only).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStoreRenameDialogResult(string OriginalFileName)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStoreRenameDialogResult: OriginalFileName={OriginalFileName}";
}

/// <summary>Result of the rotate-DEK dialog.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStoreRotateDekDialogResult(string? TargetKeyId, string? TargetKeyVersion, int BatchSize)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileStoreRotateDekDialogResult: TargetKeyId={TargetKeyId ?? "(none)"}, TargetKeyVersion={TargetKeyVersion ?? "(none)"}, BatchSize={BatchSize}";
}
