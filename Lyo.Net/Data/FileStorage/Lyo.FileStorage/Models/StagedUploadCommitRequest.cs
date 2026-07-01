namespace Lyo.FileStorage.Models;

/// <summary>Transform options for <see cref="Staged.IStagedFileUploadService.CommitAsync" /> (same semantics as ordinary saves).</summary>
public sealed class StagedUploadCommitRequest
{
    /// <summary>When true, compress through <see cref="Lyo.Compression.ICompressionService" /> during commit.</summary>
    public bool Compress { get; init; }

    /// <summary>When true, encrypt through <see cref="Lyo.Encryption.TwoKey.ITwoKeyEncryptionService" /> during commit.</summary>
    public bool Encrypt { get; init; }

    /// <summary>Required when <see cref="Encrypt" /> is true.</summary>
    public string? KeyId { get; init; }

    /// <summary>Optional path prefix override for the committed file (defaults to the stage row prefix).</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Optional streaming chunk size during commit read-from-stage.</summary>
    public int? ChunkSize { get; init; }
}