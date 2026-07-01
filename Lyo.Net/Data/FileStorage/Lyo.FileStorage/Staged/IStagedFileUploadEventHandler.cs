namespace Lyo.FileStorage.Staged;

/// <summary>Optional DI hook for host apps to publish staged-upload lifecycle signals (e.g. RabbitMQ commit workers).</summary>
public interface IStagedFileUploadEventHandler
{
    /// <summary>Invoked after <see cref="IStagedFileUploadService.BeginAsync" /> persists the stage row and issues the PUT URL.</summary>
    Task OnPresignedCreatedAsync(StagedUploadPresignedCreatedEventArgs args, CancellationToken ct = default);

    /// <summary>Invoked after <see cref="IStagedFileUploadService.CompleteAsync" /> succeeds.</summary>
    Task OnUploadCompletedAsync(StagedUploadCompletedEventArgs args, CancellationToken ct = default);

    /// <summary>Invoked when complete or commit fails.</summary>
    Task OnUploadFailedAsync(StagedUploadFailedEventArgs args, CancellationToken ct = default);

    /// <summary>Invoked after <see cref="IStagedFileUploadService.CommitAsync" /> writes canonical metadata.</summary>
    Task OnCommittedAsync(StagedUploadCommittedEventArgs args, CancellationToken ct = default);
}