using System.Diagnostics;

namespace Lyo.Compression.Models;

/// <summary><see cref="CompressionInfo" /> extended with source and destination paths for a file compress.</summary>
[DebuggerDisplay("{ToString()}")]
public sealed record FileCompressionInfo(long InputSize, long OutputSize, long TimeMs, string InputFilePath, string OutputFilePath)
    : CompressionInfo(InputSize, OutputSize, TimeMs)
{
    public override string ToString() => $"{InputFilePath} -> {OutputFilePath} {base.ToString()}";
}