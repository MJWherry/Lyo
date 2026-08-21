using System.Diagnostics;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;

namespace Lyo.FileStorage.Staged;

/// <summary>Backend-specific staging I/O (presigned PUT, object stat/read/delete). Implemented by Local/S3/Blob staged services.</summary>
public interface IStagedFilePhysicalIO
{
    /// <summary>Provider tag stored on the stage row and echoed in API responses.</summary>
    MultipartUploadProviderKind ProviderKind { get; }

    /// <summary>Builds the object/blob key or relative disk path for <c>.stage/{stageId:N}/object</c>.</summary>
    string BuildStageStorageLocation(Guid stageId, string? pathPrefix);

    /// <summary>Issues a client-upload URL (S3 presigned PUT, Azure SAS, or local API receive URL).</summary>
    Task<StagedPresignedPutResult> GeneratePresignedPutUrlAsync(
        Guid stageId,
        string normalizedPathPrefix,
        StagedUploadBeginRequest request,
        DateTimeOffset urlExpiresUtc,
        CancellationToken ct);

    /// <summary>Returns whether the staging object exists (used by complete/abort).</summary>
    Task<bool> ObjectExistsAsync(StagedFileUploadRecord record, CancellationToken ct);

    /// <summary>Returns the stored byte length after upload.</summary>
    Task<long> GetObjectSizeAsync(StagedFileUploadRecord record, CancellationToken ct);

    /// <summary>Opens a read stream for hashing during <see cref="StagedUploadCoordinator.CompleteCoreAsync" />.</summary>
    Task<Stream> OpenReadStreamAsync(StagedFileUploadRecord record, CancellationToken ct);

    /// <summary>Best-effort delete of the staging object on abort or post-commit cleanup.</summary>
    Task DeleteStageObjectAsync(StagedFileUploadRecord record, CancellationToken ct);
}

/// <summary>Presigned PUT contract returned from <see cref="IStagedFilePhysicalIO.GeneratePresignedPutUrlAsync" />.</summary>
/// <param name="Url">Absolute PUT target for the client.</param>
/// <param name="RequiredPutHeaders">Headers the client must send verbatim (SSE, content-type, Azure blob type, etc.).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StagedPresignedPutResult(string Url, IReadOnlyDictionary<string, string>? RequiredPutHeaders)
{
    /// <inheritdoc />
    public override string ToString()
        => $"StagedPresignedPutResult: Url={Url}, HeaderCount={RequiredPutHeaders?.Count ?? 0}";
}