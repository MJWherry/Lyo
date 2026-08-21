using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>stage/{stageId}/commit</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StagedUploadCommitRequest
{
    /// <summary>When true, compress during commit.</summary>
    public bool Compress { get; init; }

    /// <summary>When true, encrypt during commit.</summary>
    public bool Encrypt { get; init; }

    /// <summary>Required when <see cref="Encrypt" /> is true.</summary>
    public string? KeyId { get; init; }

    /// <summary>Optional path prefix override for the committed file.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Optional streaming chunk size during commit.</summary>
    public int? ChunkSize { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"StagedUploadCommitRequest: compress={Compress}, encrypt={Encrypt}, PathPrefix={PathPrefix ?? "(none)"}, KeyId={KeyId ?? "(none)"}";
}
