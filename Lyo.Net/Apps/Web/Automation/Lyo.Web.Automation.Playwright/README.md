# Lyo.Web.Automation.Playwright

Playwright backend for the `Lyo.Web.Automation` abstractions: launches Chromium / Firefox / WebKit, owns session-scoped browser contexts, and exposes the same tab, frame, dialog, keyboard, and locator helpers as [`Lyo.Web.Automation.Selenium`](../Lyo.Web.Automation.Selenium/README.md) so JSON automation plans and scripted runners behave the same on both engines.

## Examples

### First steps

```csharp
services.AddPlaywrightBrowserServiceFromConfiguration(builder.Configuration);

// per operation:
var service = sp.GetRequiredService<IPlaywrightBrowserService>();
using var session = service.CreateSession();
await session.StartBrowserAsync();
await session.Browser.NavigateAsync("https://example.com");
```

### Flared cookie handoff (host-owned)

```csharp
// Playwright does not reference Lyo.Http.Client. Copy UA/cookies in the host (see Lyo.TestConsole).
using var session = playwright.CreateSession(o => {
    o.BrowserKind = PlaywrightBrowserKind.Chromium;
    if (!string.IsNullOrWhiteSpace(flared.Session.UserAgent))
        o.UserAgents = [flared.Session.UserAgent];
    o.ProxyUrl = flaredOpts.ProxyUrl;
});
await session.StartBrowserAsync();
await session.Browser.CookieJar!.AddCookiesAsync(
    flared.Session.CookieJar.Export().Select(c => new BrowserCookie {
        Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path,
        Secure = c.Secure, HttpOnly = c.HttpOnly, Expiry = c.Expiry
    }));
await session.Browser.NavigateToAsync(url);
```

## Browser layer ([`Browser/`](Browser))

- **`PlaywrightBrowser`.** Concrete `IWebAutomationBrowser` (`StartBrowserAsync` boots Playwright and a default page); composes `PlaywrightBrowserTabs`, `PlaywrightDialogs`, `PlaywrightKeyboard`, `PlaywrightFrameNavigator`, `PlaywrightFrameSelectors`, `PlaywrightCookieJar`, `PlaywrightHeaderStore`.
- `PlaywrightTabManager` / `PlaywrightBrowserTabInfo`. Engine-native tab info and management.
- **`BrowserUrlRedaction`.** Query/fragment redaction used in logs when `MaskSensitiveUrlsInLogs` is set.
- `PlaywrightWebAutomationElement`. Playwright `ILocator` adapter for `IWebAutomationElement` (`ClickAsync`, `SendKeysAsync` with `Fill` / `PressSequentially`, `SendKeysRawAsync`, attribute reads, etc.).
- `PlaywrightLocatorFactory` ([`PlaywrightLocatorFactory.cs`](PlaywrightLocatorFactory.cs)). Maps `Lyo.Web.Automation` element specs onto Playwright locators.
- **Cloudflare handoff is host-owned.** This package does not reference `Lyo.Http.Client`. After a Flared solve, the host copies solver UA onto `PlaywrightBrowserOptions.UserAgents` (Chromium only), the same `ProxyUrl` onto the context, and `ILyoHttpCookieJar` entries onto `CookieJar`. Worked example: [`FlaredPlaywrightHandoff.cs`](../../../../Tools/Lyo.TestConsole/FlaredPlaywrightHandoff.cs). Architecture: [`HttpClientArchitecture.drawio`](../../../Http/Lyo.Http.Client/HttpClientArchitecture.drawio).

## Service layer ([`Service/`](Service))

- **`IPlaywrightBrowserService`.** Singleton factory: `CreateSession()` clones service config (including `BrowserKind`); `CreateSession(Action<PlaywrightBrowserOptions>)` overlays on that clone (pin UA without resetting Firefox/Chromium). `CreateSession(new PlaywrightSessionOptions())` is a full replace and defaults to Chromium. `ActiveSessionCount`, `Dispose`.
- **`IPlaywrightBrowserSession`.** Scoped session.
- `PlaywrightExecutionContextFactory` / `PlaywrightExecutionContext`. Wiring used by the automation runners.
- **`PlaywrightMetricTags`.** Constants for metric tags.

## Settings ([`Configuration/`](Configuration))

Application-wide defaults live on [`PlaywrightBrowserOptions`](Configuration/PlaywrightBrowserOptions.cs); `PlaywrightSessionOptions` is the per-session subclass passed to
`CreateSession`. Configuration section: `"PlaywrightBrowserOptions"`.

| Property | Default | Notes |
| ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BrowserKind` | `Chromium` | `PlaywrightBrowserKind`: `Chromium`, `Firefox`, `Webkit`. Firefox is Playwright Nightly. Flared handoff requires Chromium. |
| `Headless` | `false` | Launch headless where the engine allows it. |
| `Channel` | `null` | Optional Playwright channel (`msedge`, `chrome`, …). Chromium only; ignored for Firefox/WebKit. |
| `ProxyUrl` | `null` | Browser-context proxy. Copy `FlaredHttpOptions.ProxyUrl` so `cf_clearance` stays same-IP. |
| `LaunchArguments` | `--disable-infobars`, `--disable-extensions`, `--disable-dev-shm-usage`, `--no-sandbox` (Chromium only) | Every entry **must** start with `-` (`ResolveLaunchArguments` throws otherwise). Adds `--window-size` for Chromium only. Firefox/WebKit drop Chromium defaults: Playwright treats unknown tokens as start URLs (`http://1920,1080`). `--disable-gpu` is omitted (SwiftShader tell). |
| `UserAgents` | one Chrome UA on Linux | First entry is used as the context User-Agent. Firefox/WebKit skip the default Chrome UA and keep the engine UA. |
| `ViewportWidth/Height` | `1920` × `1080` | Starting layout viewport (`window.innerWidth` / `innerHeight`). |
| `ScreenWidth/Height` | `1920` × `1080` | Playwright `ScreenSize` (`window.screen`). Headless Chromium reports 800×600 unless this is set. Must be ≥ viewport. |
| `RandomizeViewportOnSessionStart` | `false` | When true, pick from `ViewportSizes` (or `DesktopDisplaySize.Common`) once at `StartBrowserAsync` and set both viewport and screen. Tests should leave this false. |
| `ViewportSizes` / `ViewportJitterPixels` | `[]` / `0` | Pool and optional ±pixel jitter for session-start randomization. Prefer discrete common sizes over jitter. |
| `NavigationWaitUntil` | `DOMContentLoaded` | Playwright `waitUntil` for goto/reload. Cloudflare interstitials often never fire `load`. Set `Load` only when every subresource must finish. |
| `NavigationTimeoutMs` | `30 000` | Default navigation timeout (ms). |
| `LocatorDefaultTimeoutMs` | `30 000` | Default timeout for locator actions and the `PollForElementAsync` family. |
| `PollingMaxAttempts` / `PollingDelayBetweenAttempts` | `5` / `500 ms` | Controls the outer poll-retry loop. |
| `EnableMetrics` | `true` | When an `IMetrics` is registered, emit instrumentation. |
| `SlowMoMilliseconds` | `0` | Delay between Playwright operations (slow motion). |
| `IgnoreHttpsErrors` | `false` | The browser context ignores HTTPS errors. |
| `CloseOwnedResourcesOnDispose` | `true` | Whether dispose closes `StartBrowserAsync` ownership. |
| `MaskSensitiveUrlsInLogs` | `false` | Drop query/fragment from log lines. |
| `ServiceRootDirectory` | `{tmp}/lyo-playwright` | Each session creates `session-{id}/` with `artifacts/`, `browser-profile/`, `downloads/`. |
| `BrowserUserDataDirectory` / `DownloadDirectory` / `ArtifactsDirectory` | derived under the session directory | Override per field if needed. |
| `Clone()` | n/a | Deep copy used to derive session-specific options. |

## DI registration ([`Service/PlaywrightServiceExtensions.cs`](Service/PlaywrightServiceExtensions.cs))

| Method | Registers |
| ---------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `AddPlaywrightBrowser(Action<PlaywrightBrowserOptions>?)` | Singleton options plus scoped `PlaywrightBrowser` for direct injection. |
| `AddPlaywrightBrowser(PlaywrightBrowserOptions)` | Same, with an explicit options instance. |
| `AddPlaywrightBrowserService(Action<PlaywrightBrowserOptions>?)` | Above plus singleton `IPlaywrightBrowserService` (use this for session-based usage via `CreateSession`). |
| `AddPlaywrightBrowserService(PlaywrightBrowserOptions)` | Same, with explicit options. |
| `AddPlaywrightBrowserServiceFromConfiguration(IConfiguration, configSectionName?)` | Binds options from configuration (default section `"PlaywrightBrowserOptions"`). |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Web.Automation` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Playwright` `1.59.0` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)