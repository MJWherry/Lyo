# Lyo.Web.Automation.Playwright

Playwright backend for the `Lyo.Web.Automation` abstractions: launches Chromium / Firefox / WebKit, owns session-scoped browser contexts, and exposes the same tab, frame, dialog, keyboard, and locator helpers as [`Lyo.Web.Automation.Selenium`](../Lyo.Web.Automation.Selenium/README.md) so JSON automation plans and scripted runners behave the same on both engines.

## Features

- **Automation studio.** `PlaywrightAutomationStudio` opens a visible browser with Record, Play, and Tools. Record turns clicks, typing, and selects into an `AutomationPlan`. The drawer starts on the left, can be dragged wider, and Save writes JSON into a directory above the plan. Stop pauses so Start can record another plan in the same window. Add step, on Record, chooses a step that does not point at the page: delay, store literal, page URL, page title, or download URLs. Tools listens to network traffic, picks images, links, text, and attributes, and drags an arrow onto text to store it in a variable. Play loads plan JSON and runs it in the same window without appending those steps. Collapse and dock clicks stay out of the plan.
- **Session factory.** `AddPlaywrightBrowserSessionFactory` registers `IAutomationBrowserSessionFactory`. `CreateSession` applies the browser kind and headless flag, and throws before a browser starts when the kind is not one Playwright can launch.

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

### Record a plan

```csharp
services.AddWebAutomationPlanRunner();
var studio = new PlaywrightAutomationStudio(playwright, runner);
await using var recording = await studio.StartAsync(new PlaywrightAutomationStudioOptions {
    StartUrl = "https://example.com",
    PlanName = "checkout"
});
// Record builds the plan. Tools aims at the page. Play runs JSON in this window.
var plan = await recording.StopAsync();
```

### Flared cookie handoff (host-owned)

```csharp
// Playwright does not reference Lyo.Http.Client. Copy UA/cookies in the host (see Lyo.TestConsole).
using var session = playwright.CreateSession(o => {
    o.BrowserKind = AutomationBrowserKind.Chromium;
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

## Automation studio ([`Record/`](Record))

- **`PlaywrightAutomationStudio`.** `StartAsync` launches the session (`Headless` defaults to false) and installs the drawer. Pass `IAutomationPlanRunner` so Play can run a plan on that session; without it, Run says play is unavailable. `OutputDirectory` is where Save writes, and it defaults to `./plans`. `StopAsync` commits pending typing, closes the browser, and returns the plan. Closing the window completes `Completion`. The drawer Stop control pauses the plan and leaves the window open. While Play is running, clicks, keys, and navigations are not appended.
- **Drawer.** Shadow-DOM panel docked to the left edge of the top window by default. Drag the inner edge to resize it. It is not installed in frames, and it stays on the viewport when the page uses transforms. Pause, Start, Stop, and Save stay above Record, Play, and Tools. The status line shows Recording, Paused, or Stopped, the dock side, and an armed extract tool. Pause holds the same plan and Play continues it. Start begins another plan. Record shows the output directory, numbered steps with up, down, and remove, and the step list: click, extract images, links, text, and attribute, drag to text, delay, store literal, page URL, page title, download URLs, capture network, and store captured URLs. JSON stays hidden until Show JSON, where it opens as a tree; Raw shows the text and Apply replaces the steps. Invalid JSON leaves them. A click or select is stored with a selector that matches that element only, so a bare `input` is not used when other inputs are on the page. Enter, Tab, and the arrow keys become `sendKeys` steps. Tools starts and stops a live network list that survives navigation and filters it with a URL regular expression. Store captured URLs on Record writes the shown URLs as `storeStringList`. Click a row to open a popup that stays the same height while you switch Headers, Request, and Response. The popup shows the status, separate request and response properties and header lists, and the body. JSON opens in a tree with the same kind colors as the JSON editor, with a Raw view. Select a value and Extract stores it on the plan. XML is indented. An image response is shown. Replay in new tab resends the request in another tab. Add capture step writes a URL regular expression onto the plan. That fills the Record tab's download form with the same variable. Extract images, links, text, and attribute arm a pick, then you click the page. Drag to text drops an arrow on a unique control and adds find-element plus extract-text; a target that is not unique is reported and does not add a step. Play has a file picker, the JSON files already in the output directory, a JSON box, and Run.
- **CLI.** [`Lyo.Web.Automation.Studio`](../Lyo.Web.Automation.Studio) opens the window and registers the plan runner so Play works. `--out` is an optional directory (default `./plans`). Save from the drawer after Stop. Close the browser or press Enter to exit.

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
| ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BrowserKind` | `Chromium` | `AutomationBrowserKind`: `Chrome`, `Chromium`, `Edge`, `Firefox`, `WebKit`. Chrome and Edge launch Chromium with channel `chrome` or `msedge`. Firefox is Playwright Nightly. Flared handoff allows Chrome, Chromium, and Edge. |
| `Headless` | `false` | Launch headless where the engine allows it. |
| `Channel` | `null` | When empty, Chrome uses channel `chrome` and Edge uses `msedge`. An explicit channel wins for Chrome, Chromium, and Edge. Ignored for Firefox and WebKit. |
| `ProxyUrl` | `null` | Browser-context proxy. Copy `FlaredHttpOptions.ProxyUrl` so `cf_clearance` stays same-IP. |
| `LaunchArguments` | `--disable-infobars`, `--disable-extensions`, `--disable-dev-shm-usage`, `--no-sandbox` (Chrome, Chromium, and Edge) | Every entry **must** start with `-` (`ResolveLaunchArguments` throws otherwise). Adds `--window-size` for Chrome, Chromium, and Edge. Firefox and WebKit drop Chromium defaults: Playwright treats unknown tokens as start URLs (`http://1920,1080`). `--disable-gpu` is omitted (SwiftShader tell). |
| `UserAgents` | empty | An empty list is generated for `BrowserKind` on this host (`UserAgentGenerator`). A list the host sets is used as-is; the first entry is the context User-Agent. |
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
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)