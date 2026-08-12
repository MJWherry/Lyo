# Lyo.ContentThreatScan

Heuristic scanning and numeric disposition scoring for **readable text** payloads (scripts, markup, suspicious SQL-ish patterns).

## Pieces

| Type                                                          | Role                                                                                                                                                                          |
|---------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IContentThreatScanner` / `DefaultContentThreatScanner`       | UTF-8–bounded sample rules and per-category scoring. Required ctor arg: `ContentThreatHeuristicOptions`.                                                                      |
| `ContentThreatAssessment`                                     | Aggregate result. Exposes `HeuristicScore`, `ExternalScore`, total `Score`, `IntelConfirmedMalicious`, and the `IReadOnlyList<ContentThreatContribution>` that produced them. |
| `ContentThreatAssessmentOptions`                              | Suspect/threat thresholds, disposition cap, `ForceThreatOnConfirmedIntel`, plus `FailureBumpPoints` / `FailureContributionRuleId` for provider-failure scoring.               |
| `ContentThreatHeuristicOptions`                               | Eligibility (default text extensions / content-types, MIME-sniffing flags), `MaxBytesToAnalyze`, per-category score caps, binary-skip toggle.                                 |
| `ContentThreatScanContext`                                    | Per-call metadata passed into heuristic and reputation probes: filename, content-type, correlation id, optional caller fields.                                                |
| `ContentThreatCategory`                                       | Enum of rule families that produce hits (`SqlInjection`, `Script`, `Reputation`, `AntiVirus`, etc.).                                                                          |
| `ContentThreatContribution`                                   | One rule hit: category, weighted points, rule id, optional snippet. Build aggregate scores via `ContentThreatAssessment.FromContributions`.                                   |
| `ContentThreatDisposition` / `ContentThreatDispositionMapper` | Maps a numeric score to `Clean` / `Suspect` / `Threat` bands using `ContentThreatAssessmentOptions` thresholds.                                                               |
| `ExternalReputationEnvelope`                                  | Result returned by `IContentThreatReputationPipeline`: per-provider scores, intel-confirmed flag, failures.                                                                   |
| `ContentThreatReputationRequest`                              | Input to reputation pipelines: 32-byte SHA-256 digest, optional `LimitedSamplePrefix`, file-type hint, `ContentThreatScanContext`.                                            |
| `IContentThreatReputationPipeline`                            | Optional lookups (implementations live in **Lyo.ContentThreatScan.Intel**).                                                                                                   |
| `ContentThreatAssessmentComposer`                             | Merges heuristic contributions with the external reputation envelope, applies thresholds, returns the final `ContentThreatAssessment` + disposition.                          |

## Sampling and digests

`ContentThreatBuffering` exposes bounded async reads (`ReadLimitedAsync`) and `ComputeSha256` for a stable 32-byte digest passed to reputation pipelines alongside an optional
capped sample prefix (`ContentThreatReputationRequest`).

## File storage bridge

The **[Lyo.FileStorage](../../../Data/FileStorage/Lyo.FileStorage/)** package includes `ContentThreatMalwareScanner` implementing `IFileMalwareScanner` by composing heuristics,
optional reputation, thresholds, and `FileScanThreatLevel`.

## Registration (typical DI)

Wire `IContentThreatScanner` as `DefaultContentThreatScanner`. For lookup-only reputation, register **`IContentThreatReputationPipeline`**: - **
`NullContentThreatReputationPipeline.Instance`** — no outbound calls. For HTTP-backed reputation, prefer a **typed / named `HttpClient`** registered against
`DefaultContentThreatReputationPipeline` plus `ReputationPipelineOptions` bound from configuration (timeouts, failure dispositions, API keys). Do not enable unsolicited
request-body middleware in consuming apps unless policy explicitly calls for it.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Hashing` — (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)