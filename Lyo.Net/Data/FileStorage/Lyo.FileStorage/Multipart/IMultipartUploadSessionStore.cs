namespace Lyo.FileStorage.Multipart;

public interface IMultipartUploadSessionStore
{
    Task CreateAsync(MultipartUploadSessionRecord session, CancellationToken ct = default);

    Task<MultipartUploadSessionRecord?> GetAsync(Guid sessionId, CancellationToken ct = default);

    Task UpdateProviderStateAsync(Guid sessionId, string providerStateJson, CancellationToken ct = default);

    Task SetStatusAsync(Guid sessionId, MultipartSessionStatus status, CancellationToken ct = default);

    Task DeleteAsync(Guid sessionId, CancellationToken ct = default);
}