# Lyo.Web.Automation.Selenium

Selenium WebDriver backend for the `Lyo.Web.Automation` abstractions: launch Chrome / Edge / Firefox / Safari (plus Selenium Grid), isolate sessions, and cover polling, tab/frame/dialog/keyboard helpers, typed element controls, automation plans, and DI registration.

## Examples

### First steps

```csharp
services.AddSeleniumBrowserServiceFromConfiguration(builder.Configuration);

// later, per-operation:
var service = sp.GetRequiredService<ISeleniumBrowserService>();
using var session = service.CreateSession();
await session.StartBrowserAsync();
await session.Browser.NavigateAsync("https://example.com");
```

## Browser layer ([`Browser/`](Browser))

- **`SeleniumBrowser`.** Concrete `IWebAutomationBrowser` (`PageBase`); composes `SeleniumBrowserTabs`, `BrowserAlerts`, `BrowserKeyboard`, `FrameNavigator`, `SeleniumPolling`.
- **`SeleniumWebAutomationElement`.** `IWebElement` adapter for `IWebAutomationElement`.
- `SeleniumBrowserTabs` / `TabManager`. Abstract-tab and engine-native management.
- **`BrowserCookieExtensions`.** Typed cookie helpers.
- **`BrowserUrlRedaction`.** Fragment/query-string redaction used in logs when `MaskSensitiveUrlsInLogs` is set.

## Service layer ([`Service/`](Service))

- **`ISeleniumBrowserService`.** Singleton factory: `CreateSession(SeleniumSessionOptions?)`, `ActiveSessionCount`, `Dispose`.
- **`ISeleniumBrowserSession`.** Scoped session (`Tabs`, `Browser`, dispose to release WebDriver resources).
- `SeleniumExecutionContextFactory` / `SeleniumExecutionContext`. Wiring used by the automation runners.
- **`SeleniumMetricTags`.** Constants for metric tags.

## Settings ([`Configuration/`](Configuration))

Application-wide defaults live on [`SeleniumBrowserOptions`](Configuration/SeleniumBrowserOptions.cs); `SeleniumSessionOptions` is the per-session subclass passed to
`CreateSession`. Configuration section: `"SeleniumBrowserOptions"`.

| Property | Default | Notes |
| ----------------------------------------------------------------------- | ------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BrowserKind` | `Chrome` | `SeleniumBrowserKind`: `Chrome`, `Edge`, `Firefox`, `Safari`. |
| `Headless` | `false` | Headless mode where the engine allows it. |
| `RemoteWebDriverUri` | `null` | When set, drives `RemoteWebDriver` against a standalone server / Selenium Grid. |
| `UserAgents` | 4 desktop UAs | Cycled for stealth. |
| `WebDriverArguments` | `disable-infobars`, `disable-extensions`, `disable-dev-shm-usage`, `no-sandbox` | Use `AddArgument(key, value?)` to append `key=value` (or bare key) entries. `disable-gpu` is omitted: it forces SwiftShader, a bot-detection tell. |
| `StartupScripts` | `[]` | JS injected via CDP `Page.addScriptToEvaluateOnNewDocument` (Edge/Chrome only). Bypass bot-detection traps. |
| `EnablePerformanceLogging` | `true` | Required for CDP-based network observation in `IWebAutomationNavigator.NavigateAsync`. Disable on sites that detect CDP, then provide a JS interception script in `StartupScripts`. |
| `BrowserWindowWidth/Height` | `1920` × `1080` | Starting window size (`--window-size` plus `Manage().Window.Size`). |
| `RandomizeViewportOnSessionStart` | `false` | When true, pick from `ViewportSizes` (or `DesktopDisplaySize.Common`) once at `StartBrowserAsync`. |
| `ViewportSizes` / `ViewportJitterPixels` | `[]` / `0` | Pool and optional ±pixel jitter for session-start randomization. |
| `PageLoadTimeoutSeconds` | `30` | Timeout for page load. |
| `ImplicitWaitSeconds` | `10` | Implicit wait on WebDriver. |
| `ScriptTimeoutSeconds` | `30` | Timeout for async scripts. |
| `SeleniumMaxWaitSeconds` | `15` | Wrapper around explicit waits. |
| `EnableMetrics` | `true` | When an `IMetrics` is registered, emit instrumentation. |
| `MaskSensitiveUrlsInLogs` | `false` | Drop query/fragment from log lines. |
| `ServiceRootDirectory` | `{tmp}/lyo-selenium` | Each session creates `session-{id}/` with `artifacts/`, `browser-profile/`, `downloads/`. |
| `BrowserUserDataDirectory` / `DownloadDirectory` / `ArtifactsDirectory` | derived under the session directory | Override per field if needed. |
| `PollingMaxAttempts` / `PollingDelayBetweenAttempts` | `5` / `500 ms` | Outer-loop control for `SeleniumBrowser.PollFor`. |
| `Clone()` | n/a | Deep copy used to derive session-specific options. |

A fluent builder over the options record is `SeleniumBrowserOptionsBuilder`.

## Typed element controls ([`Controls/`](Controls))

Wrappers over `IWebElement` for typed interactions: `WebElementControl` (base), `InputControl` (`SendKeys`, `Value`), `TextAreaControl`, `ButtonControl`, `LinkControl`, `SelectControl`, `CheckboxControl`. Resolve via `SeleniumBrowser` element APIs and hold them across explicit waits.

## WebDriver and automation ([`WebDriver/`](WebDriver), [`Automation/ElementLocatorMapping.cs`](Automation/ElementLocatorMapping.cs))

Driver-factory helpers (Edge / Chrome / Safari / Firefox + Remote) and the locator mapping that lets `Lyo.Web.Automation` JSON automation plans target Selenium-style locators.

## DI registration ([`Service/Extensions.cs`](Service/Extensions.cs))

Every registration is an `IServiceCollection` extension:

| Method | Registers |
| -------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `AddSeleniumBrowser(Action<SeleniumBrowserOptions>?)` | Singleton `SeleniumBrowserOptions`, scoped `SeleniumBrowser` for direct injection (legacy style). |
| `AddSeleniumBrowser(Action<SeleniumBrowserOptionsBuilder>)` | Same, with options built fluently. |
| `AddSeleniumBrowser(SeleniumBrowserOptions)` | Same, with an explicit options instance. |
| `AddSeleniumBrowserService(Action<SeleniumBrowserOptions>?)` | Above plus singleton `ISeleniumBrowserService` (use this for session-based usage via `CreateSession`). |
| `AddSeleniumBrowserService(Action<SeleniumBrowserOptionsBuilder>)` | Same, fluent options. |
| `AddSeleniumBrowserService(SeleniumBrowserOptions)` | Same, explicit options. |
| `AddSeleniumBrowserServiceFromConfiguration(IConfiguration, sectionName?)` | Binds options from configuration (default section `"SeleniumBrowserOptions"`). |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Web.Automation` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Selenium.Support` `4.46.0` (direct, third-party)
- `Selenium.WebDriver` `4.46.0` (direct, third-party)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)