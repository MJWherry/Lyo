# Lyo.Web.Automation.Playwright

Playwright implementation of the engine-agnostic `Lyo.Web.Automation` abstractions: launches Chromium / Firefox / WebKit, manages session-scoped browser contexts, and exposes
the same tab/frame/dialog/keyboard/locator surface as [`Lyo.Web.Automation.Selenium`](../Lyo.Web.Automation.Selenium/README.md) so JSON automation plans and scripted runners
behave identically across both engines.

## Examples

### Quick start

```csharp
services.AddPlaywrightBrowserServiceFromConfiguration(builder.Configuration);

// per operation:
var service = sp.GetRequiredService<IPlaywrightBrowserService>();
using var session = service.CreateSession();
await session.StartBrowserAsync();
await session.Browser.NavigateAsync("https://example.com");
```

## Surface — Browser ([`Browser/`](Browser))

- **`PlaywrightBrowser`** — concrete `IWebAutomationBrowser` (`StartBrowserAsync` boots Playwright, optionally a persistent context, and a default page); composes `PlaywrightBrowserTabs`, `PlaywrightDialogs`, `PlaywrightKeyboard`, `PlaywrightFrameNavigator`, `PlaywrightFrameSelectors`, `PlaywrightCookieJar`, `PlaywrightHeaderStore`.
- **`PlaywrightBrowserTabInfo`** / **`PlaywrightTabManager`** — engine-native tab info and management.
- **`BrowserUrlRedaction`** — query/fragment redaction used in logs when `MaskSensitiveUrlsInLogs` is set.
- **`PlaywrightWebAutomationElement`** — `IWebAutomationElement` adapter on top of Playwright `ILocator` (`ClickAsync`, `SendKeysAsync` with `Fill` / `PressSequentially`, `SendKeysRawAsync`, attribute reads, etc.).
- **`PlaywrightLocatorFactory`** ([`PlaywrightLocatorFactory.cs`](PlaywrightLocatorFactory.cs)) — converts `Lyo.Web.Automation` element specs to Playwright locators.

## Surface — Service ([`Service/`](Service))

- **`IPlaywrightBrowserService`** — singleton factory: `CreateSession(PlaywrightSessionOptions?)`, `ActiveSessionCount`, `Dispose`.
- **`IPlaywrightBrowserSession`** — scoped session.
- **`PlaywrightExecutionContext`** / `PlaywrightExecutionContextFactory` — wiring used by the automation runners.
- **`PlaywrightMetricTags`** — metric-tag constants.

## Surface — Configuration ([`Configuration/`](Configuration))

[`PlaywrightBrowserOptions`](Configuration/PlaywrightBrowserOptions.cs) holds application-wide defaults; `PlaywrightSessionOptions` is the per-session subclass passed to
`CreateSession`. Configuration section: `"PlaywrightBrowserOptions"`.

| Property | Default | Notes |
| ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `BrowserKind` | `Chromium` | `PlaywrightBrowserKind`: `Chromium`, `Firefox`, `Webkit`. |
| `Headless` | `false` | Headless launch where supported. |
| `Channel` | `null` | Optional Playwright channel (`chrome`, `msedge`, …). |
| `LaunchArguments` | `--disable-infobars`, `--disable-extensions`, `--disable-gpu`, `--disable-dev-shm-usage`, `--no-sandbox` | Each entry **must** start with `-`; Playwright treats bare tokens as initial URLs. |
| `UserAgents` | one Linux Chrome UA | Round-robined for stealth. |
| `ViewportWidth/Height` | `1280` × `1024` | Initial viewport in CSS pixels. |
| `NavigationTimeoutMs` | `30 000` | Default navigation timeout (ms). |
| `LocatorDefaultTimeoutMs` | `30 000` | Default timeout for locator actions and `PollForElementAsync` family. |
| `PollingMaxAttempts` / `PollingDelayBetweenAttempts` | `5` / `500 ms` | Outer-loop control for poll retries. |
| `EnableMetrics` | `true` | Emit `IMetrics` instrumentation when an `IMetrics` is registered. |
| `SlowMoMilliseconds` | `0` | Slow-motion delay between Playwright operations. |
| `IgnoreHttpsErrors` | `false` | Ignores HTTPS errors in the browser context. |
| `CloseOwnedResourcesOnDispose` | `true` | Whether `StartBrowserAsync` ownership is closed on dispose. |
| `MaskSensitiveUrlsInLogs` | `false` | Strip query/fragment in log lines. |
| `ServiceRootDirectory` | `{tmp}/lyo-playwright` | Each session creates `session-{id}/` with `browser-profile/`, `artifacts/`, `downloads/`. |
| `BrowserUserDataDirectory` / `DownloadDirectory` / `ArtifactsDirectory` | derived under session dir | Override individually if needed. |
| `Clone()` | — | Deep copy used to derive session-specific options. |

## DI registration ([`Service/PlaywrightServiceExtensions.cs`](Service/PlaywrightServiceExtensions.cs))

| Method | Registers |
| ---------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `AddPlaywrightBrowser(Action<PlaywrightBrowserOptions>?)` | Singleton options + scoped `PlaywrightBrowser` for direct injection. |
| `AddPlaywrightBrowser(PlaywrightBrowserOptions)` | Same, with explicit options instance. |
| `AddPlaywrightBrowserService(Action<PlaywrightBrowserOptions>?)` | Above + singleton `IPlaywrightBrowserService` (use this for session-based usage via `CreateSession`). |
| `AddPlaywrightBrowserService(PlaywrightBrowserOptions)` | Same, explicit options. |
| `AddPlaywrightBrowserServiceFromConfiguration(IConfiguration, configSectionName?)` | Binds options from configuration (default section `"PlaywrightBrowserOptions"`). |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.IO.Temp` — (direct, lyo)
- `Lyo.Web.Automation` — (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Playwright` `1.59.0` — (direct, microsoft)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Formatter` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)