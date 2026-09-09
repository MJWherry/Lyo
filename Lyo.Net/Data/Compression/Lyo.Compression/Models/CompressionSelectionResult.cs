namespace Lyo.Compression.Models;

/// <summary>Result of <see cref="ICompressionAlgorithmSelector.ResolveForCompress" />.</summary>
/// <param name="ShouldCompress">When <see langword="false" />, the save path skips compression even if the caller asked for <c>compress: true</c>.</param>
/// <param name="Algorithm">Codec to use when <paramref name="ShouldCompress" /> is <see langword="true" />.</param>
public sealed record CompressionSelectionResult(bool ShouldCompress, CompressionAlgorithm? Algorithm);