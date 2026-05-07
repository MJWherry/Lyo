# Lyo.ContentThreatScan.Intel

Optional **`DefaultContentThreatReputationPipeline`** for Malware Bazaar, VirusTotal, and **`clamd` INSTREAM** (TCP).

## Composition

Construct **`DefaultContentThreatReputationPipeline`** with a shared **`HttpClient`** (often from **`IHttpClientFactory`**) and **`ReputationPipelineOptions`** bound from
configuration (API keys, timeouts, cache sizes, Clam TCP settings, and per-provider failure dispositions).

Register the instance as **`IContentThreatReputationPipeline`** wherever **`ContentThreatMalwareScanner`** or other hosts need reputation.

Probes are omitted when keys are absent: empty **`VirusTotalApiKey`** skips VT; empty **`MalwareBazaarAuthKey`** skips Bazaar; **`Clamd.Enabled == false`** skips `clamd`.

## Outages and quotas

Configure per-provider **`ExternalReputationFailureDisposition`** on **`ReputationPipelineOptions`**:

- **`Ignore`** — swallow (logged); no score bump
- **`TreatAsSuspect`** — adds `ProviderFailureSuspectBump` under a stable rule id
- **`ImmediateThreatBump`** — large contribution capped by disposition options (policy-driven “fail closed”)

Separate fields exist for Bazaar, VirusTotal, and **`Clamd.FailureDisposition`**.

## Digest cache

`ReputationDigestLookupCache` is an in-process LRU keyed by lowercase hex SHA-256 (`DigestCacheMaximumEntries`, positive/negative TTL minutes).
