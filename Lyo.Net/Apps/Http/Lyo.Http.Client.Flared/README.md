# Lyo.Http.Client.Flared

HTTP-only Cloudflare clearance via a running **FlareSolverr** instance (`/v1`) through FlareSolverrSharp. Not an in-process browser. Default **ThroughSolver** because ClearanceHandler replay uses .NET TLS and often fails (`cookies … are not valid`).

`HttpClient` never loads a page's subresources. Extract image/AWS URLs from HTML with `ThenExtractImages`, or hand cookies to Playwright `NavigateAsync` for XHR-only URLs. FlareSolverr does not return its Chrome network log.

Start FlareSolverr locally: `docker compose --profile flared up -d flaresolverr` (does not start with the bench `run` profile).

## Features

- **ThroughSolver.** Map `HttpRequestMessage` → `request.get` / `request.post`; synthetic response from `solution.response`; merge cookies; pin `solution.userAgent`.
- **Downloads.** Large binaries skip the JSON envelope; stream with cookies+UA after clearance (`LyoHttpRequestMarkers.StreamBinary`).
- **Stealth knobs on the base pipeline.** Timing 200–800 ms + jitter, rate limit 1/2s, PerSession UA. `PerRequest` rotation fails `Validate()`.
- **Named client `Lyo.Http.Flared`.** Own handler and rate-limit bucket. Replay mode: `ConfigurePrimaryHttpMessageHandler(() => new ClearanceHandler(...))` — new instance each time.

## Examples

### Register and run

```csharp
services.AddFlaredHttpClientFromConfiguration(configuration);
// FlaredHttpOptions.FlareSolverrUrl defaults to http://127.0.0.1:8191/
```

### Compose

```bash
docker compose --profile flared up -d flaresolverr
```

## TLS and replay

Stay on ThroughSolver so TLS is FlareSolverr's Chrome. Document replay TLS mismatch. No curl-impersonate or JA3 forgers. Do not invent `Sec-Fetch-*` on replay.

## Bot-detection fingerprints

FlareSolverr's Docker image ships Chromium with dummy GPU packages and Xvfb. Sites that inspect `window.screen` often report **800×600**, and WebGL reports **SwiftShader Device (Subzero)**. The `/v1` API cannot set viewport or GPU. Do not spoof `WEBGL_UNMASKED_RENDERER` — it will not match the rest of the stack. For pages that fingerprint the renderer, solve with Flared then hand cookies to Playwright (host GPU, `ScreenSize` set, no `--disable-gpu`).

## Playwright handoff and VPN IP

Diagrams: [`HttpClientArchitecture.drawio`](../Lyo.Http.Client/HttpClientArchitecture.drawio) (packages, ThroughSolver vs StreamBinary, Playwright handoff).

`cf_clearance` is bound to IP plus User-Agent. FlareSolverr in Docker (`docker compose --profile flared`) is not your host VPN exit. Set `FlaredHttpOptions.ProxyUrl` (applied to FlareSolverr and to StreamBinary/.NET fetches) and `PlaywrightBrowserOptions.ProxyUrl` to the same exit. Playwright does not reference this package — the host copies solver UA onto `PlaywrightBrowserOptions.UserAgents` before `StartBrowserAsync` and cookies onto `CookieJar` after start. Worked example: [`FlaredPlaywrightHandoff.cs`](../../../Tools/Lyo.TestConsole/FlaredPlaywrightHandoff.cs). Require `BrowserKind` Chromium; Firefox Nightly under automation cannot take Flared cookies. Navigate with `NavigationWaitUntil` = `DOMContentLoaded` (Cloudflare often never fires `load`).

## Links

- [FlareSolverr](https://github.com/FlareSolverr/FlareSolverr)
- [FlareSolverrSharp](https://github.com/FlareSolverr/FlareSolverrSharp)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `FlareSolverrSharp` `3.0.7` (direct, third-party)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)