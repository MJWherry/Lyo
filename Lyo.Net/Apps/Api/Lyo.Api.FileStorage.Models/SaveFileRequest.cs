using System.Diagnostics;
using System.Text.Json;

namespace Lyo.Api.FileStorage.Models;

/// <summary>JSON body posted to <c>files/save</c> (file bytes travel in-band).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SaveFileRequest(
    byte[] Data,
    string? OriginalFileName = null,
    bool Compress = false,
    bool Encrypt = false,
    string? KeyId = null,
    string? PathPrefix = null,
    int? ChunkSize = null,
    string? ContentType = null,
    string? Charset = null,
    string? TenantId = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"SaveFileRequest: OriginalFileName={OriginalFileName ?? "(none)"}, Bytes={Data.Length}, compress={Compress}, encrypt={Encrypt}";

    /// <summary>Opaque caller JSON bag. Lyo does not interpret keys. Null when omitted.</summary>
    public JsonElement? Metadata { get; init; }
}
