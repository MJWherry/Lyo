namespace Lyo.ContentThreatScan;

/// <summary>Lets hash reputation providers reuse the same SHA256 digest producers already computed by hosts; optional prefix enables clamd INSTREAM.</summary>
public sealed class ContentThreatReputationRequest
{
    public ReadOnlyMemory<byte> Sha256Digest32 { get; }

    /// <summary>Optional bounded plaintext prefix for streaming AV scanners (clamd).</summary>
    public ReadOnlyMemory<byte>? LimitedSamplePrefix { get; }

    public ContentThreatReputationRequest(ReadOnlyMemory<byte> sha256Digest32, ReadOnlyMemory<byte>? limitedSamplePrefix = null)
    {
        if (sha256Digest32.Length != 32)
            throw new ArgumentException("SHA256 digest must be exactly 32 bytes.", nameof(sha256Digest32));

        Sha256Digest32 = sha256Digest32;
        LimitedSamplePrefix = limitedSamplePrefix;
    }
}