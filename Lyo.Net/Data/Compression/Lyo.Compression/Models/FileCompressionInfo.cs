using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString()}")]
public sealed record FileCompressionInfo(long InputSize, long OutputSize, long TimeMs, string InputFilePath, string OutputFilePath)
    : CompressionInfo(InputSize, OutputSize, TimeMs)
{
    public override string ToString() => $"{InputFilePath} -> {OutputFilePath} {base.ToString()}";
}