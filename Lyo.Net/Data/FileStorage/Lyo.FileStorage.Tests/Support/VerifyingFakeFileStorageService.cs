using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.Health;

namespace Lyo.FileStorage.Tests.Support;

/// <summary>
/// In-memory backend standing in for S3 or Blob on the direct-upload finalize path. <see cref="ReportedAttributes" /> plays the part of the server-side size and
/// checksum; leave it null to model a backend that holds no checksum. Body reads are counted and can be made to throw, which is how the tests prove finalize never
/// pulled the object back through the host.
/// </summary>
public sealed class VerifyingFakeFileStorageService(FileStorageServiceBaseOptions options, IFileMetadataStore metadataStore, IEnumerable<IFileAuditEventHandler>? auditHandlers = null)
    : FileStorageServiceBase(options, metadataStore, auditHandlers: auditHandlers), IDirectUploadVerification
{
    private readonly Dictionary<Guid, byte[]> _objects = [];

    /// <summary>What <see cref="IDirectUploadVerification.GetDirectUploadAttributesAsync" /> returns. Null means the backend cannot report a checksum.</summary>
    public DirectUploadObjectAttributes? ReportedAttributes { get; set; }

    /// <summary>Algorithm this fake claims to verify on write, passed to begin so the matching client digest is recorded.</summary>
    public HashAlgorithm? VerifiedChecksumAlgorithm { get; set; }

    /// <summary>When true, any full-body read throws.</summary>
    public bool ThrowOnBodyRead { get; set; }

    public int BodyReadCount { get; private set; }

    public List<int> PrefixRequests { get; } = [];

    /// <summary>Stands in for the client's PUT to the presigned URL.</summary>
    public void PutObject(Guid fileId, byte[] bytes) => _objects[fileId] = bytes;

    public bool HasObject(Guid fileId) => _objects.ContainsKey(fileId);

    public override async Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default)
    {
        var fileId = LyoGuid.CreateV7();
        await PersistPendingPlainDirectUploadMetadataAsync(fileId, request, "", ct, VerifiedChecksumAlgorithm);
        return new() {
            FileId = fileId,
            PresignedPutUrl = $"https://fake.invalid/{fileId:N}",
            UrlExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
            StorageLocation = fileId.ToString("N")
        };
    }

    public override Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => FinalizePendingPlainDirectUploadCoreAsync(fileId, completeRequest, ct);

    Task<DirectUploadObjectAttributes?> IDirectUploadVerification.GetDirectUploadAttributesAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
        => Task.FromResult(_objects.ContainsKey(fileId) ? ReportedAttributes : null);

    Task<byte[]> IDirectUploadVerification.ReadDirectUploadPrefixAsync(Guid fileId, string? pathPrefix, int maxBytes, CancellationToken ct)
    {
        PrefixRequests.Add(maxBytes);
        var bytes = _objects[fileId];
        return Task.FromResult(bytes.AsSpan(0, Math.Min(maxBytes, bytes.Length)).ToArray());
    }

    protected override Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero, null, null));

    protected override Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct) => throw new NotSupportedException();

    protected override Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => Task.FromResult(_objects.TryGetValue(fileId, out var bytes) ? bytes.LongLength : 0L);

    protected override Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        if (ThrowOnBodyRead)
            throw new InvalidOperationException("Finalize must not download the uploaded object when the backend reports its checksum.");

        BodyReadCount++;
        return Task.FromResult<Stream?>(_objects.TryGetValue(fileId, out var bytes) ? new MemoryStream(bytes, false) : null);
    }

    protected override Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => Task.FromResult(_objects.Remove(fileId));

    protected override Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => throw new NotSupportedException();

    protected override Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
        => throw new NotSupportedException();

    protected override Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => Task.CompletedTask;
}
