# Lyo.Web.Automation.Selenium

Selenium WebDriver implementation of the engine-agnostic `Lyo.Web.Automation` abstractions: browser launch (Chrome / Edge / Firefox / Safari + Selenium Grid), session
isolation, polling, tab/frame/dialog/keyboard helpers, typed element controls, automation plans, and DI registration.

## Examples

### Quick start

```csharp
services.AddSeleniumBrowserServiceFromConfiguration(builder.Configuration);

// later, per-operation:
var service = sp.GetRequiredService<ISeleniumBrowserService>();
using var session = service.CreateSession();
await session.StartBrowserAsync();
await session.Browser.NavigateAsync("https://example.com");
```

## Surface — Browser ([`Browser/`](Browser))

- **`SeleniumBrowser`** — concrete `IWebAutomationBrowser` (`PageBase`); composes `SeleniumBrowserTabs`, `BrowserAlerts`, `BrowserKeyboard`, `FrameNavigator`, `SeleniumPolling`.
- **`SeleniumWebAutomationElement`** — `IWebAutomationElement` adapter on top of `IWebElement`.
- **`TabManager`** / **`SeleniumBrowserTabs`** — engine-native and abstract-tab management.
- **`BrowserCookieExtensions`** — typed cookie helpers.
- **`BrowserUrlRedaction`** — query-string/fragment redaction used in logs when `MaskSensitiveUrlsInLogs` is set.

## Surface — Service ([`Service/`](Service))

- **`ISeleniumBrowserService`** — singleton factory: `CreateSession(SeleniumSessionOptions?)`, `ActiveSessionCount`, `Dispose`.
- **`ISeleniumBrowserSession`** — scoped session (`Browser`, `Tabs`, dispose to release WebDriver resources).
- **`SeleniumExecutionContext`** / `SeleniumExecutionContextFactory` — wiring used by the automation runners.
- **`SeleniumMetricTags`** — metric-tag constants.

## Surface — Configuration ([`Configuration/`](Configuration))

[`SeleniumBrowserOptions`](Configuration/SeleniumBrowserOptions.cs) holds application-wide defaults; `SeleniumSessionOptions` is the per-session subclass passed to
`CreateSession`. Configuration section: `"SeleniumBrowserOptions"`.

| Property | Default | Notes |
| ----------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BrowserKind` | `Chrome` | `SeleniumBrowserKind`: `Chrome`, `Edge`, `Firefox`, `Safari`. |
| `Headless` | `false` | Headless mode where supported. |
| `RemoteWebDriverUri` | `null` | When set, drives `RemoteWebDriver` against a Selenium Grid / standalone server. |
| `UserAgents` | 4 desktop UAs | Round-robined for stealth. |
| `WebDriverArguments` | `disable-infobars`, `disable-extensions`, `disable-gpu`, `disable-dev-shm-usage`, `no-sandbox` | Use `AddArgument(key, value?)` to append `key=value` (or bare key) entries. |
| `StartupScripts` | `[]` | JS injected via CDP `Page.addScriptToEvaluateOnNewDocument` (Chrome/Edge only) — bypass bot-detection traps. |
| `EnablePerformanceLogging` | `true` | Required for CDP-based network observation in `IWebAutomationNavigator.NavigateAsync`. Disable on sites that detect CDP, then provide a JS interception script in `StartupScripts`. |
| `BrowserWindowWidth/Height` | `1280` × `1024` | Initial window size. |
| `PageLoadTimeoutSeconds` | `30` | Page-load timeout. |
| `ImplicitWaitSeconds` | `10` | WebDriver implicit wait. |
| `ScriptTimeoutSeconds` | `30` | Async-script timeout. |
| `SeleniumMaxWaitSeconds` | `15` | Wrapper for explicit waits. |
| `EnableMetrics` | `true` | Emit `IMetrics` instrumentation when an `IMetrics` is registered. |
| `MaskSensitiveUrlsInLogs` | `false` | Strip query/fragment in log lines. |
| `ServiceRootDirectory` | `{tmp}/lyo-selenium` | Each session creates `session-{id}/` with `browser-profile/`, `artifacts/`, `downloads/`. |
| `BrowserUserDataDirectory` / `DownloadDirectory` / `ArtifactsDirectory` | derived under session dir | Override individually if needed. |
| `PollingMaxAttempts` / `PollingDelayBetweenAttempts` | `5` / `500 ms` | Outer-loop control for `SeleniumBrowser.PollFor`. |
| `Clone()` | — | Deep copy used to derive session-specific options. |

`SeleniumBrowserOptionsBuilder` is a fluent builder over the options record.

## Surface — Typed element controls ([`Controls/`](Controls))

Wrappers over `IWebElement` for typed interactions: `WebElementControl` (base), `InputControl` (`Value`, `SendKeys`), `TextAreaControl`, `ButtonControl`, `LinkControl`, `SelectControl`, `CheckboxControl`. Resolve via `SeleniumBrowser` element APIs and hold them across explicit waits.

## Surface — WebDriver / Automation ([`WebDriver/`](WebDriver), [`Automation/ElementLocatorMapping.cs`](Automation/ElementLocatorMapping.cs))

Driver-factory helpers (Chrome / Edge / Firefox / Safari + Remote) and the locator mapping that lets `Lyo.Web.Automation` JSON automation plans target Selenium-style locators.

## DI registration ([`Service/Extensions.cs`](Service/Extensions.cs))

All registrations are extension methods on `IServiceCollection`:

| Method | Registers |
| -------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `AddSeleniumBrowser(Action<SeleniumBrowserOptions>?)` | Singleton `SeleniumBrowserOptions`, scoped `SeleniumBrowser` for direct injection (legacy style). |
| `AddSeleniumBrowser(Action<SeleniumBrowserOptionsBuilder>)` | Same, with options built fluently. |
| `AddSeleniumBrowser(SeleniumBrowserOptions)` | Same, with explicit options instance. |
| `AddSeleniumBrowserService(Action<SeleniumBrowserOptions>?)` | Above + singleton `ISeleniumBrowserService` (use this for session-based usage via `CreateSession`). |
| `AddSeleniumBrowserService(Action<SeleniumBrowserOptionsBuilder>)` | Same, fluent options. |
| `AddSeleniumBrowserService(SeleniumBrowserOptions)` | Same, explicit options. |
| `AddSeleniumBrowserServiceFromConfiguration(IConfiguration, sectionName?)` | Binds options from configuration (default section `"SeleniumBrowserOptions"`). |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.IO.Temp` — (direct, lyo)
- `Lyo.Web.Automation` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (direct, microsoft)
- `Selenium.Support` `4.46.0` — (direct, third-party)
- `Selenium.WebDriver` `4.46.0` — (direct, third-party)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Formatter` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)