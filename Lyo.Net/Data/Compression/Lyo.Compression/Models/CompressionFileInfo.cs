namespace Lyo.Compression.Models;

public sealed record CompressionFileInfo(long UncompressedSize, long CompressedSize, long TimeMs, string InputFilePath, string OutputFilePath)
    : CompressionInfo(UncompressedSize, CompressedSize, TimeMs)
{
    public override string ToString() => $"{InputFilePath} -> {OutputFilePath} {base.ToString()}";
}