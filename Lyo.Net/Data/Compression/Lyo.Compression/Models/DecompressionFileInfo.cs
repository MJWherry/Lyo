namespace Lyo.Compression.Models;

public sealed record DecompressionFileInfo(long CompressedSize, long DecompressedSize, long TimeMs, string InputFilePath, string OutputFilePath)
    : DecompressionInfo(CompressedSize, DecompressedSize, TimeMs)
{
    public override string ToString() => $"{InputFilePath} -> {OutputFilePath} {base.ToString()}";
}