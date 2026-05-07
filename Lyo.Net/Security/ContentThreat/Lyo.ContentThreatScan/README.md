# Lyo.ContentThreatScan

Heuristic scanning and numeric disposition scoring for **readable text** payloads (scripts, markup, suspicious SQL-ish patterns).

## Pieces

| Type                                                    | Role                                                                       |
|---------------------------------------------------------|----------------------------------------------------------------------------|
| `IContentThreatScanner` / `DefaultContentThreatScanner` | UTF-8–bounded sample rules and per-category scoring                        |
| `ContentThreatAssessment`                               | Summed disposition score + `IntelConfirmedMalicious` flag                  |
| `ContentThreatAssessmentOptions`                        | Suspect/threat thresholds, disposition cap, `ForceThreatOnConfirmedIntel`  |
| `IContentThreatReputationPipeline`                      | Optional lookups (implementations live in **Lyo.ContentThreatScan.Intel**) |
| `ContentThreatAssessmentComposer`                       | Merges heuristic contributions with external envelope                      |

## Sampling and digests

`ContentThreatBuffering` exposes bounded async reads (`ReadLimitedAsync`) and `ComputeSha256` for a stable 32-byte digest passed to reputation pipelines alongside an optional
capped sample prefix (`ContentThreatReputationRequest`).

## File storage bridge

The **[Lyo.FileStorage](../../../Data/FileStorage/Lyo.FileStorage/)** package includes `ContentThreatMalwareScanner` implementing `IFileMalwareScanner` by composing heuristics,
optional reputation, thresholds, and `FileScanThreatLevel`.

## Registration (typical DI)

Wire `IContentThreatScanner` as `DefaultContentThreatScanner`. For lookup-only reputation, register **`IContentThreatReputationPipeline`**:

- **`NullContentThreatReputationPipeline.Instance`** — no outbound calls.

For HTTP-backed reputation, prefer a **typed / named `HttpClient`** registered against `DefaultContentThreatReputationPipeline` plus `ReputationPipelineOptions` bound from
configuration (timeouts, failure dispositions, API keys).

Do not enable unsolicited request-body middleware in consuming apps unless policy explicitly calls for it.
