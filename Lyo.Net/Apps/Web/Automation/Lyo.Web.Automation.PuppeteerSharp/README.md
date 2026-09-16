# Lyo.Web.Automation.PuppeteerSharp

PuppeteerSharp backend for the `Lyo.Web.Automation` abstractions. It launches Chrome, Chromium, Edge, or Firefox, owns a session-scoped browser context, and exposes the same tab, frame, dialog, keyboard, and locator helpers as [`Lyo.Web.Automation.Playwright`](../Lyo.Web.Automation.Playwright/README.md) so JSON automation plans behave the same on this engine. WebKit is rejected. This package does not reference `Lyo.Http.Client` or `Lyo.Web.WebRenderer` and does not download a browser.

## Features

- **`PuppeteerSharpBrowser`.** Concrete `IWebAutomationBrowser`. `StartBrowserAsync` launches `BrowserKind` from `BrowserExePath` or `AutomationBrowserDetector` and opens one page in a browser context.
- `PuppeteerSharpTabManager` / `PuppeteerSharpBrowserTabInfo`. Engine-native tab info. Portable tab switching stays on `IWebAutomationTabs`.
- `PuppeteerSharpWebAutomationElement` and `PuppeteerSharpLocatorFactory`. Map `ElementLocator` (id, name, css, xpath, link text, class, tag) onto Puppeteer selectors.
- **Cloudflare handoff is host-owned.** Copy the solver user-agent onto `UserAgents`, `ProxyUrl` onto the context, and cookies onto `CookieJar`. This package does not reference `Lyo.Http.Client`.

## Examples

### First steps

```csharp
services.AddPuppeteerSharpBrowserServiceFromConfiguration(builder.Configuration);

var service = sp.GetRequiredService<IPuppeteerSharpBrowserService>();
using var session = service.CreateSession();
await session.StartBrowserAsync();
await session.Browser.NavigateAsync("https://example.com");
```

## Service layer ([`Service/`](Service))

- **`IPuppeteerSharpBrowserService`.** `CreateSession()` clones service options. `CreateSession(Action<PuppeteerSharpBrowserOptions>)` overlays that clone. `CreateSession(new PuppeteerSharpSessionOptions())` replaces it. `ActiveSessionCount`, `Dispose`.
- **`IPuppeteerSharpBrowserSession`.** Session scope. `Tabs` is `PuppeteerSharpTabManager`.
- `AddPuppeteerSharpBrowser`, `AddPuppeteerSharpBrowserService`, and `AddPuppeteerSharpBrowserServiceFromConfiguration`. The options-instance overloads call `Validate()`.

## Settings ([`Configuration/PuppeteerSharpBrowserOptions.cs`](Configuration/PuppeteerSharpBrowserOptions.cs))

| Property | Default | Notes |
| ---------------------------------- | ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BrowserKind` | `Chrome` | `AutomationBrowserKind`: `Chrome`, `Chromium`, `Edge`, `Firefox`. WebKit is rejected. Firefox drops Chromium launch-arg defaults. |
| `BrowserExePath` | `null` | When empty, launch uses `AutomationBrowserDetector` for `BrowserKind` (that browser's env vars, then `BROWSER_PATH` and `PUPPETEER_EXECUTABLE_PATH`, then install paths). An explicit path wins. Launch throws when the file is missing. No browser download. |
| `Headless` | `false` | Launch headless when true. |
| `ServiceRootDirectory` | `{tmp}/lyo-puppeteersharp` | Each session creates `session-{id}/` with `browser-profile/`, `artifacts/`, and `downloads/`. |
| `LaunchArguments` | Chromium defaults including `--no-sandbox` | Every entry must start with `-`. `ResolveLaunchArguments` adds `--window-size` for Chrome, Chromium, and Edge. Firefox omits those defaults and `--window-size`. |
| `ViewportWidth` / `ViewportHeight` | `1920` × `1080` | CSS viewport. `ApplySessionViewport` can randomize from `ViewportSizes`. |
| `NavigationWaitUntil` | `DOMContentLoaded` | Puppeteer `waitUntil` for goto and reload. |
| `NavigationTimeoutMs` | `30000` | Navigation timeout. |
| `LocatorDefaultTimeoutMs` | `30000` | Element wait used by each poll attempt. |
| `EnableMetrics` | `true` | Records `lyo.webautomation.puppeteersharp.*` when an `IMetrics` is registered. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Web.Automation` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `PuppeteerSharp` `24.0.0` (direct, third-party)
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