# Lyo.ContentThreatScan.ClamAv

ClamAV provider for content-threat scoring. `ClamAvContentThreatScan` calls `IClamAvClient` and returns an `ExternalReputationEnvelope` for `ContentThreatAssessmentComposer`.

**Archetype B (capability provider).** Register `AddClamAvClient` first, then `AddClamAvContentThreatScan`.

An infected verdict adds `clamav.detected` (`AntiMalwareEngine`) and, by default, sets intel-confirmed so the disposition mapper returns threat. A clean verdict adds nothing. An engine error or `LyoHttpException` follows `FailureDisposition`: ignore, a suspect bump (`clamav.failure`), or a threat bump (`clamav.failure.threat`).

`ScanAsync` posts the caller stream. The file name is `ContentThreatScanContext.OriginalFileName`, or `upload.bin` when that is missing. `InspectAsync` scans `LimitedSamplePrefix` when those bytes exist and skips the client when they do not.

## Features

- **Infected.** One contribution `clamav.detected`, category `AntiMalwareEngine`, points from `DetectionPoints` (default 118). `MarksIntelConfirmed` defaults to true.
- **Clean.** Empty envelope.
- **Failure.** `Ignore` (default) leaves the envelope empty. `TreatAsSuspect` adds `clamav.failure` for `SuspectBump` points (default 16). `ImmediateThreatBump` adds `clamav.failure.threat` for `ThreatBump` points (default 132). Failure bumps are category `Reputation` and do not confirm intel.
- **Registration.** `AddClamAvContentThreatScan`, the options instance overload, and `AddClamAvContentThreatScanFromConfiguration`. Registers transient `ClamAvContentThreatScan` and `IContentThreatReputationPipeline`.

## Examples

### Register and score

```csharp
services.AddClamAvClient(o => o.BaseUrl = "http://127.0.0.1:8080/");
services.AddClamAvContentThreatScan();

var envelope = await scan.ScanAsync(stream, new ContentThreatScanContext("upload.bin"), ct);
var assessment = ContentThreatAssessmentComposer.Compose(heuristics, envelope, new ContentThreatAssessmentOptions());
var disposition = ContentThreatDispositionMapper.Resolve(assessment, new ContentThreatAssessmentOptions());
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.ClamAv.Client` (direct, lyo)
- `Lyo.ContentThreatScan` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.RateLimit` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)