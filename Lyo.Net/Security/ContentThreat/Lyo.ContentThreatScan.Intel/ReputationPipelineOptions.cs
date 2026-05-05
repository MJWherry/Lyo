namespace Lyo.ContentThreatScan.Intel;

/// <summary>How HTTP/TCP outages are translated into disposition score.</summary>
public enum ExternalReputationFailureDisposition
{
    /// <summary>Ignore failure (additive score 0, log).</summary>
    Ignore = 0,

    /// <summary>Add <see cref="ReputationPipelineOptions.ProviderFailureSuspectBump"/> points.</summary>
    TreatAsSuspect = 1,

    /// <summary>Add a large contribution capped by disposition ceiling (forces threat).</summary>
    ImmediateThreatBump = 2
}

/// <summary>Configurable keys, quotas, weights, caches, ClamTCP and failure hooks for <see cref="DefaultContentThreatReputationPipeline"/>.</summary>
public sealed class ReputationPipelineOptions
{
    public Uri MalwareBazaarEndpoint { get; set; } = new("https://mb-api.abuse.ch/api/v1/", UriKind.Absolute);

    /// <summary>Maps to Abuse.ch <c>Auth-Key</c> header.</summary>
    public string? MalwareBazaarAuthKey { get; set; }

    public Uri VirusTotalApiRoot { get; set; } = new("https://www.virustotal.com/api/v3/", UriKind.Absolute);


    /// <summary>VirusTotal personal API keys are required for lookups.</summary>
    public string? VirusTotalApiKey { get; set; }

    public TimeSpan ProviderTimeout { get; set; } = TimeSpan.FromSeconds(20);

    public int NegativeCacheMinutes { get; set; } = 180;

    public int PositiveMalwareCacheMinutes { get; set; } = 7 * 24 * 60;

    public int DigestCacheMaximumEntries { get; set; } = 768;

    public decimal MalwareBazaarKnownSamplePoints { get; set; } = 92m;

    public decimal VirusTotalPointsPerMaliciousEngine { get; set; } = 4m;

    public int VirusTotalMinimumMaliciousEnginesForIntelConfirmation { get; set; } = 4;

    public ExternalReputationFailureDisposition MalwareBazaarFailureDisposition { get; set; } = ExternalReputationFailureDisposition.Ignore;

    public ExternalReputationFailureDisposition VirusTotalFailureDisposition { get; set; } = ExternalReputationFailureDisposition.Ignore;

    public decimal ProviderFailureSuspectBump { get; set; } = 16m;

    public decimal ProviderFailureThreatBump { get; set; } = 132m;

    public ClamdInstreamScanOptions Clamd { get; set; } = new();
}

/// <summary>Optional clamd INSTREAM probe over TCP.</summary>
public sealed class ClamdInstreamScanOptions
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 3310;

    public int TcpConnectTimeoutMilliseconds { get; set; } = 5000;

    public decimal EngineDetectionPoints { get; set; } = 118m;

    public bool EngineDetectionMarksIntelConfirmed { get; set; } = true;

    public ExternalReputationFailureDisposition FailureDisposition { get; set; } = ExternalReputationFailureDisposition.Ignore;

    /// <summary>INSTREAM framing chunk writes (excluding zero terminator).</summary>
    public int InstreamChunkSize { get; set; } = 8192;
}
