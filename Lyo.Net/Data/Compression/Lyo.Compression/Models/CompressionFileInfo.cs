namespace Lyo.Compression.Models;

/// <summary>Per-file result entry for dictionary batch compress APIs.</summary>
public sealed record CompressionFileInfo(long UncompressedSize, long CompressedSize, long TimeMs, string InputFilePath, string OutputFilePath)
    : CompressionInfo(UncompressedSize, CompressedSize, TimeMs)
{
    public override string ToString() => $"{InputFilePath} -> {OutputFilePath} {base.ToString()}";
}