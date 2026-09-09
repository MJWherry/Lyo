using System.Diagnostics;

namespace Lyo.FileStorage.Multipart;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MultipartPartDescriptor(int PartNumber, string? PresignedPutUrl, string HttpMethod = "PUT")
{
    /// <inheritdoc />
    public override string ToString() => $"MultipartPartDescriptor: PartNumber={PartNumber}, HttpMethod={HttpMethod}";
}
