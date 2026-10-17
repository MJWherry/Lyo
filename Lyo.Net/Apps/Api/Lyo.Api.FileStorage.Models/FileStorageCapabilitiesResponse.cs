using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>
/// <c>GET capabilities</c> body. Flags a client can use to enable or disable actions. Bucket, disk root, and keystore location are not included.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageCapabilitiesResponse(
    bool PresignedRead,
    bool DirectUpload,
    bool Multipart,
    bool PresignedMultipartParts,
    bool Copy,
    bool Move,
    bool Physical,
    bool Encryption,
    bool Archive,
    bool AccessLinks,
    IReadOnlyList<FileStorageTagTypeResponse>? TagTypes = null,
    IReadOnlyList<FileStorageTagValueKindResponse>? TagValueKinds = null)
{
    /// <summary>Every flag false. Used before a client has loaded the server.</summary>
    public static FileStorageCapabilitiesResponse None { get; } = new(false, false, false, false, false, false, false, false, false, false);

    /// <inheritdoc />
    public override string ToString()
        => "FileStorageCapabilitiesResponse: "
           + $"PresignedRead={PresignedRead} DirectUpload={DirectUpload} Multipart={Multipart} PresignedMultipartParts={PresignedMultipartParts} "
           + $"Copy={Copy} Move={Move} Physical={Physical} Encryption={Encryption} Archive={Archive} AccessLinks={AccessLinks}";
}
