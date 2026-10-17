using Lyo.Exceptions;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Multipart;
using Lyo.KeyStore;
using ApiModels = Lyo.Api.FileStorage.Models;

namespace Lyo.Api.FileStorage;

/// <summary>Builds <see cref="ApiModels.FileStorageCapabilitiesResponse" /> from the keyed storage stack.</summary>
internal static class FileStorageCapabilitiesComposer
{
    /// <summary>
    /// Combines backend flags with host services. <paramref name="multipart" /> that is <see cref="LocalMultipartUploadService" /> does not offer presigned part URLs.
    /// </summary>
    public static ApiModels.FileStorageCapabilitiesResponse Compose(
        IFileStorageService storage,
        IMultipartUploadService? multipart,
        bool encryption,
        bool archive,
        bool accessLinks,
        IReadOnlyList<ApiModels.FileStorageTagTypeResponse>? tagTypes = null,
        IReadOnlyList<ApiModels.FileStorageTagValueKindResponse>? tagValueKinds = null)
    {
        ArgumentHelpers.ThrowIfNull(storage);
        var features = storage is FileStorageServiceBase service
            ? service.GetCapabilities()
            : new FileStorageCapabilities(false, false, false, false, storage is IFileStoragePhysical);
        var multipartOn = multipart != null;
        return new(
            features.PresignedRead,
            features.DirectUpload,
            multipartOn,
            multipartOn && multipart is not LocalMultipartUploadService,
            features.Copy,
            features.Move,
            features.Physical,
            encryption,
            archive,
            accessLinks,
            tagTypes,
            tagValueKinds);
    }

    /// <summary>True when a keyed store, an unkeyed <see cref="IKeyStore" />, or an <see cref="IKeyInventoryStore" /> is registered.</summary>
    public static bool HasEncryptionStore(IKeyStore? keyed, IKeyStore? unkeyed, IKeyInventoryStore? inventory)
        => keyed != null || unkeyed != null || inventory != null;
}
