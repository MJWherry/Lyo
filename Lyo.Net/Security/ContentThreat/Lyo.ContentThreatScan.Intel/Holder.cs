namespace Lyo.ContentThreatScan.Intel;

internal sealed class Holder(ExternalReputationEnvelope envelope, DateTime expiryUtc)
{
    public ExternalReputationEnvelope Envelope { get; } = envelope;

    public DateTime ExpiryUtc { get; } = expiryUtc;
}