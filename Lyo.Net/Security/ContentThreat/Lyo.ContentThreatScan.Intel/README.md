# Lyo.ContentThreatScan.Intel

Optional **`DefaultContentThreatReputationPipeline`** for Malware Bazaar, VirusTotal, and **`clamd` INSTREAM** (TCP).

## Composition

Construct **`DefaultContentThreatReputationPipeline`** with a shared **`HttpClient`** (often from **`IHttpClientFactory`**), `ReputationPipelineOptions` bound from configuration,
and an optional `ILogger`. Register the instance as **`IContentThreatReputationPipeline`** wherever **`ContentThreatMalwareScanner`** or other hosts need reputation.

Probes are omitted when keys are absent: empty **`VirusTotalApiKey`** skips VT; empty **`MalwareBazaarAuthKey`** skips Bazaar; **`Clamd.Enabled == false`** skips `clamd`.

## `ReputationPipelineOptions`

| Property                                                   | Notes                                                                                                                                                                 |
|------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `MalwareBazaarAuthKey` / `VirusTotalApiKey`                | API keys; provider is skipped when blank.                                                                                                                             |
| `MalwareBazaarEndpoint` / `VirusTotalApiRoot`              | Override default base URLs (sovereign clouds, internal proxies).                                                                                                      |
| `ProviderTimeout`                                          | Per-call HTTP timeout.                                                                                                                                                |
| `MalwareBazaarKnownSamplePoints`                           | Score added when MalwareBazaar reports a known sample.                                                                                                                |
| `VirusTotalPointsPerMaliciousEngine`                       | Score multiplied by the number of engines flagging the sample.                                                                                                        |
| `VirusTotalMinimumMaliciousEnginesForIntelConfirmation`    | Threshold to flip `IntelConfirmedMalicious`.                                                                                                                          |
| `ProviderFailureSuspectBump` / `ProviderFailureThreatBump` | Score bumps applied when a provider's `ExternalReputationFailureDisposition` is `TreatAsSuspect` / `ImmediateThreatBump`.                                             |
| `DigestCacheMaximumEntries`                                | Maximum entries in `ReputationDigestLookupCache`.                                                                                                                     |
| `NegativeCacheMinutes` / `PositiveMalwareCacheMinutes`     | Negative and positive TTLs used by the digest cache.                                                                                                                  |
| `Clamd` (`ClamdInstreamScanOptions`)                       | `Enabled`, `Host`, `Port`, `TcpConnectTimeoutMilliseconds`, `InstreamChunkSize`, `EngineDetectionPoints`, `EngineDetectionMarksIntelConfirmed`, `FailureDisposition`. |

## Outages and quotas

Configure per-provider **`ExternalReputationFailureDisposition`** on **`ReputationPipelineOptions`**:

- **`Ignore`** — swallow (logged); no score bump
- **`TreatAsSuspect`** — adds `ProviderFailureSuspectBump` under a stable rule id
- **`ImmediateThreatBump`** — large contribution capped by disposition options (policy-driven “fail closed”)

Separate fields exist for Bazaar, VirusTotal, and **`Clamd.FailureDisposition`**.

## Digest cache

`ReputationDigestLookupCache` is an in-process LRU keyed by lowercase hex SHA-256 (`DigestCacheMaximumEntries`, positive/negative TTL minutes).
