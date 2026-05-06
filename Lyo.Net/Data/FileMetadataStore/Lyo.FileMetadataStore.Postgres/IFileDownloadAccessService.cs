namespace Lyo.FileMetadataStore.Postgres;

public interface IFileDownloadAccessService
{
    Task<CreateFileDownloadAccessLinkResult> CreateLinkAsync(CreateFileDownloadAccessLinkRequest request, CancellationToken ct = default);

    Task<ConsumeFileDownloadAccessLinkResult> ValidateAndConsumeDownloadAsync(string token, string? actorId = null, string? ipAddress = null, DateTime? nowUtc = null,
        CancellationToken ct = default);
}
