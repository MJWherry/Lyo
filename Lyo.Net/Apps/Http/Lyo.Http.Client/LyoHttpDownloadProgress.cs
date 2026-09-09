namespace Lyo.Http.Client;

/// <summary>Progress for a streaming download.</summary>
public readonly record struct LyoHttpDownloadProgress(long BytesReceived, long? TotalBytes)
{
    /// <summary>Fraction in 0–1 when <see cref="TotalBytes" /> is known.</summary>
    public double? Fraction => TotalBytes is > 0 ? (double)BytesReceived / TotalBytes.Value : null;
}
