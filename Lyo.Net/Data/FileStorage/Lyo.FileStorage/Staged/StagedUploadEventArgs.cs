using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Staged;

/// <summary>Event payload for <see cref="IStagedFileUploadService.PresignedCreated" /> and <see cref="IStagedFileUploadEventHandler.OnPresignedCreatedAsync" />.</summary>
public sealed class StagedUploadPresignedCreatedEventArgs : EventArgs
{
    public required Guid StageId { get; init; }

    public string? TenantId { get; init; }

    public required StagedFileResult Snapshot { get; init; }
}

/// <summary>Event payload for <see cref="IStagedFileUploadService.UploadCompleted" /> and <see cref="IStagedFileUploadEventHandler.OnUploadCompletedAsync" />.</summary>
public sealed class StagedUploadCompletedEventArgs : EventArgs
{
    public required Guid StageId { get; init; }

    public string? TenantId { get; init; }

    public required StagedFileResult Snapshot { get; init; }
}

/// <summary>Event payload for <see cref="IStagedFileUploadService.UploadFailed" /> and <see cref="IStagedFileUploadEventHandler.OnUploadFailedAsync" />.</summary>
public sealed class StagedUploadFailedEventArgs : EventArgs
{
    public required Guid StageId { get; init; }

    public string? TenantId { get; init; }

    public StagedFileResult? Snapshot { get; init; }

    public string? ErrorMessage { get; init; }
}

/// <summary>Event payload for <see cref="IStagedFileUploadService.Committed" /> and <see cref="IStagedFileUploadEventHandler.OnCommittedAsync" />.</summary>
public sealed class StagedUploadCommittedEventArgs : EventArgs
{
    public required Guid StageId { get; init; }

    public string? TenantId { get; init; }

    public required Guid CommittedFileId { get; init; }

    public required FileStoreResult FileResult { get; init; }
}
