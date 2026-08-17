namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

/// <summary>Result of the move or copy path-prefix dialog.</summary>
public sealed record FileStorePathPrefixDialogResult(string? PathPrefix);

/// <summary>Result of the rename dialog (metadata display name only).</summary>
public sealed record FileStoreRenameDialogResult(string OriginalFileName);

/// <summary>Result of the rotate-DEK dialog.</summary>
public sealed record FileStoreRotateDekDialogResult(string? TargetKeyId, string? TargetKeyVersion, int BatchSize);
