using Lyo.Compression.Models;

namespace Lyo.Compression;

/// <summary>
/// Selects whether and how to compress a payload on write using file type, size, tenant, environment, and configuration. File storage and other orchestrators invoke
/// <see cref="ICompressionService.ResolveForCompress" /> when <c>compress: true</c>; the chosen algorithm is applied via <see cref="ICompressionService.Resolver" /> and stored in
/// metadata.
/// </summary>
public interface ICompressionAlgorithmSelector
{
    /// <summary>Resolves compression for a single save operation.</summary>
    CompressionSelectionResult ResolveForCompress(CompressionSelectionContext context);
}