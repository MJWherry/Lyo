using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Abstractions;

/// <summary>
/// Implemented by storage backends that can describe a client-uploaded object without transferring its body. When a <see cref="FileStorageServiceBase" /> subclass
/// implements this, <see cref="IFileStorageService.CompleteDirectUploadAsync" /> finalizes from the reported size and checksum instead of downloading the object back
/// through the host to hash it. Backends that cannot report a checksum keep the download-and-hash path.
/// </summary>
public interface IDirectUploadVerification
{
    /// <summary>
    /// Reports the stored length and a checksum the backend computed or verified on write, for the pending object behind <paramref name="fileId" />. Returns
    /// <see langword="null" /> when the object is missing or the backend holds no checksum for it; finalize then falls back to reading the object.
    /// </summary>
    Task<DirectUploadObjectAttributes?> GetDirectUploadAttributesAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Reads at most <paramref name="maxBytes" /> from the start of the object, for file-signature checks. Returns fewer bytes when the object is shorter.</summary>
    Task<byte[]> ReadDirectUploadPrefixAsync(Guid fileId, string? pathPrefix, int maxBytes, CancellationToken ct);
}
