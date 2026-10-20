using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>One named store from <c>GET {Route}/stores</c>. <see cref="FileMetadataRoute" /> stays null until metadata routes are per store.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageStoreResponse(string Name, string Route, string? FileMetadataRoute)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStorageStoreResponse: {Name} {Route}";
}
