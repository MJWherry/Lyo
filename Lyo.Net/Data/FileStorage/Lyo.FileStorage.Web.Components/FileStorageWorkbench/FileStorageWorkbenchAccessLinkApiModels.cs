namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

public sealed record FileStorageWorkbenchCreateAccessLinkRequest(
    DateTime? NotBeforeUtc = null,
    DateTime? ExpiresAtUtc = null,
    DateTime? WindowStartUtc = null,
    DateTime? WindowEndUtc = null,
    int? MaxDownloads = null,
    string? TenantId = null);

public sealed record FileStorageWorkbenchAccessLinkResponse(Guid LinkId, string Token, string DownloadUrl, string PresignedReadUrl, DateTime CreatedUtc, DateTime? ExpiresAtUtc);