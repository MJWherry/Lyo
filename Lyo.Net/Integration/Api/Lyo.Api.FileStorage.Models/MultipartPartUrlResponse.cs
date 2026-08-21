using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>GET <c>multipart/{sessionId}/part-url</c> payload.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MultipartPartUrlResponse(int PartNumber, string? PresignedPutUrl, string HttpMethod = "PUT")
{
    /// <inheritdoc />
    public override string ToString() => $"MultipartPartUrlResponse: PartNumber={PartNumber}, HttpMethod={HttpMethod}";
}
