using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Multipart;

public interface IMultipartUploadService
{
    Task<MultipartBeginResult> BeginAsync(MultipartBeginRequest request, CancellationToken ct = default);

    Task<MultipartPartDescriptor> GetPresignedPartUploadAsync(Guid sessionId, int partNumber, CancellationToken ct = default);

    /// <summary>Server-side part upload for providers that do not expose presigned PUT URLs (e.g. local disk staging).</summary>
    Task UploadPartAsync(Guid sessionId, int partNumber, Stream content, CancellationToken ct = default);

    Task<FileStoreResult> CompleteAsync(CompleteMultipartUploadRequest request, CancellationToken ct = default);

    Task AbortAsync(Guid sessionId, CancellationToken ct = default);
}