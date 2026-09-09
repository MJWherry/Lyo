using System.Diagnostics;

namespace Lyo.Compression.Models;

/// <summary><see cref="DecompressionInfo" /> plus paths for a file decompress.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileDecompressionInfo(long InputSize, long OutputSize, long TimeMs, string InputFilePath, string OutputFilePath)
    : DecompressionInfo(InputSize, OutputSize, TimeMs)
{
    public override string ToString() => $"{InputFilePath} -> {OutputFilePath} {base.ToString()}";
}