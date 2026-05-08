# Lyo.Web.Automation

**Serializable automation plans** and a shared **runner** for **Selenium** and **Playwright** via `IWebAutomationSession` / `IWebAutomationBrowser`. The browser façade composes *
*`IWebAutomationNavigator`** (navigate / reload), **`IWebAutomationPage`** (find elements, source, URL/title, viewport PNG, **`SetViewportSizeAsync`**), **`IWebAutomationTabs`** (
list/switch/open/close tabs via opaque keys), cookies, and optional extra headers—the same instance exposes **`Navigator`**, **`CurrentPage`**, and **`Tabs`** for narrower
dependencies.

| Project                         | Role                                                                                         |
|---------------------------------|----------------------------------------------------------------------------------------------|
| `Lyo.Web.Automation`            | Plan types, `AutomationPlanBuilder`, `IAutomationPlanRunner`, interpolation, hooks / metrics |
| `Lyo.Web.Automation.Selenium`   | Selenium-backed session and `SeleniumBrowser`                                                |
| `Lyo.Web.Automation.Playwright` | Playwright-backed session and browser adapter                                                |

Plans are **ordered lists of steps** (`AutomationPlan`). Steps can navigate, reload, **resize viewport/window**, **switch/open/close tabs**, find elements (single or lists), act on
elements, extract text or attributes into **string variables**,
write files, download URL lists/single files, call HTTP APIs, extract image source URLs, upload local directories through file storage, upsert JSON payloads through a data sink,
and store literals or page metadata. String values can be combined with
**`{{variableName}}`** placeholders resolved at run time.

---

## Concepts

### `AutomationPlan`

- **`Name`**: Optional label for logging and persistence.
- **`Steps`**: **`IReadOnlyList<AutomationStepDefinition>`** — immutable after **`AutomationPlanBuilder.Build()`** (defensive copy). Each step is a polymorphic record; if you
  serialize to JSON yourself, a typical shape discriminates on **`type`**.

Build in code with **`AutomationPlanBuilder`** (assigns a time-ordered **`StepId`** on every step when you did not set one). Deserialize with **`System.Text.Json`** (or anything
else) in your host — the library does not ship a serializer.

**Viewport / tab JSON discriminators** (polymorphic `type`): **`setViewportSize`** (`width`, `height`), **`switchTabByIndex`** (`tabIndex`), **`switchTabByKey`** (`tabKey`), *
*`openNewTab`** (`url`, `tabKeyVariableName`), **`closeCurrentTab`**.

### Locators and chains

- **`ElementLocator`**: One strategy (`ElementLocatorKind`: `Id`, `CssSelector`, `XPath`, …) plus a **`Value`**.
- **`ElementLocatorChain`**: One or more segments, outer → inner (nested find / chained Playwright locator). Construct with `new ElementLocatorChain(a, b)` or fluently:
  `ElementLocator.CssSelector("#app").Then(ElementLocator.CssSelector("button"))`.

Single-segment chains are equivalent to a simple find; multi-segment steps use chain-specific step types in JSON (`findElementChain`, `findElementsChain`, …).

### Browser façade (`IWebAutomationBrowser`)

- **`Navigator`** (`IWebAutomationNavigator`): `NavigateAsync`, `ReloadAsync`.
- **`CurrentPage`** (`IWebAutomationPage`): `PollForElementAsync` / `GetElementAsync`, `GetPageSourceAsync`, `GetCurrentUrlAsync`, `GetTitleAsync`, `TakeViewportSnapshotPngAsync`,
  **`SetViewportSizeAsync`**, etc. Applies to the **active tab/window** and **current iframe stack** (after frame navigation). Distinct from Playwright's native `IPage`. Selenium *
  *`SetViewportSizeAsync`** adjusts the OS window size, not necessarily the CSS layout viewport.
- **`Tabs`** (`IWebAutomationTabs`): `ListTabsAsync`, `SwitchToTabAsync` (by index or opaque **`TabKey`**), `OpenNewTabAsync`, `CloseCurrentTabAsync`, `SetTabDisplayNameAsync`. Tab
  keys are **opaque** (Selenium window handle vs Playwright page id).
- **`CookieJar`** / **`ExtraHeaders`**: optional engine capabilities as today.

**Typed sessions** (`ISeleniumBrowserSession` / `IPlaywrightBrowserSession`) still expose engine-native **`Tabs`** pointing at **`SeleniumBrowser.NativeTabs`** (`TabManager`) or *
*`PlaywrightBrowser.NativeTabs`** (`PlaywrightTabManager`) for advanced operations (predicate switches, window vs tab, etc.).

You can still call navigation and page methods directly on `IWebAutomationBrowser`; it inherits the narrower interfaces.

### Variables and bindings

During a run, the engine maintains:

| Binding                   | Meaning                                                 |
|---------------------------|---------------------------------------------------------|
| **Element refs**          | Named `IWebAutomationElement` instances from find steps |
| **Element list refs**     | Named lists from `findElementsChain`                    |
| **String variables**      | From extract / store / page URL / title steps           |
| **String list variables** | From list extract steps                                 |

**Interpolation** (`AutomationPlanInterpolation.ExpandAsync` during a run): templates in navigate URLs, store steps, element actions, file paths, etc. resolve against **live
bindings** — the same information you see later in **`AutomationPlanBindings`** / **`Context.Overall`**, not only pre-stored string variables. Optional *
*`AutomationPlanRuntimeOptions.Formatter`** (**`Lyo.Formatter.IFormatterService`**) validates the template with SmartFormat (same engine as **`FormatterService`**) before
resolution.

| Form                                     | Meaning                                                                                         |
|------------------------------------------|-------------------------------------------------------------------------------------------------|
| `{name}` or `{{name}}`                   | String variable `name` (legacy double braces are normalized to single). Same as `strings.name`. |
| `{strings.x}` / `{str.x}`                | String variable `x`.                                                                            |
| `{lists.x}` / `{list.x}`                 | String-list variable `x`, lines joined with newlines.                                           |
| `{page.url}`, `{page.title}`             | Current document URL / title (live from the browser).                                           |
| `{elements.ref.text}` or `{el.ref.text}` | Visible text of element ref `ref`.                                                              |
| `{elements.ref.attr.href}`               | Attribute on element ref `ref`.                                                                 |

If the entire selector matches a string variable key (including keys with dots), that value is used first. A SmartFormat format specifier after `:` is ignored for resolution (only
the part before `:` is used as the selector). Legacy synchronous **`Expand`** only supports simple `{{name}}` from a string dictionary (for callers outside the runner).

Avoid stray `{…}` in templates except for placeholders—anything between `{` and `}` is treated as a selector.

### Execution results

**`IAutomationPlanRunner.RunWithResultAsync`** returns **`AutomationPlanRunResult`**:

- **`Snapshot`**: Final **`Strings`** and **`StringLists`** only (no element handles) — convenient for logging or APIs.
- **`Context`**: Full picture:
    - **`Context.Overall`**: **`AutomationPlanBindings`** after the **last** step — **preferred** for reading final element refs and all variables.
    - **`Context.Frames`**: One **`AutomationPlanStepFrame`** per completed step (historical snapshots). Use when you need state **as it was after step *n*** (for example,
      comparing a URL list before a later step overwrote it).

Frame index **`i`** is state **after** `plan.Steps[i]` completed (zero-based).

---

## Runtime options

**`AutomationPlanRuntimeOptions`** is **not** part of the serialized plan. Pass it to **`RunWithResultAsync`** when needed:

| Property                     | Use                                                                                                                                                                                                                                                                                                                               |
|------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`DownloadFileNamePrefix`** | Default file name prefix when a step does not set one (runner default is `download`).                                                                                                                                                                                                                                             |
| **`PlanTimeout`**            | Optional ceiling for the **entire** run (combined with the run `CancellationToken`).                                                                                                                                                                                                                                              |
| **`DefaultStepTimeout`**     | Optional default per-step limit; a step can override with **`AutomationStepDefinition.StepTimeout`**.                                                                                                                                                                                                                             |
| **`Hooks`**                  | **`BeforeStepAsync`**, **`AfterStepAsync`**, **`OnFailureAsync`** (`AutomationPlanHooks`).                                                                                                                                                                                                                                        |
| **`Instrumentation`**        | Optional **`IAutomationPlanInstrumentation`** for metrics / tracing (run and step lifecycle).                                                                                                                                                                                                                                     |
| **`PlanRunDirectory`**       | Optional **`AutomationPlanRunDirectoryOptions`**: per-run folder under **`RootDirectory`** (see layout below). Set **`WriteRunLogFile`**, **`WriteSnapshots`**, and **`WriteVariables`** to **`false`** to reserve only the directory (or disable each category independently). When **`null`**, no run-scoped files are written. |
| **`Formatter`**              | Optional **`Lyo.Formatter.IFormatterService`**: validates step templates with SmartFormat before placeholders are resolved. Use single-brace placeholders (e.g. `{page.url}`) or legacy `{{page.url}}` (normalized to single braces). Register **`FormatterService`** from DI if you use this.                                    |
| **`LinkResolutionBaseUri`**  | Optional base URL used to resolve relative links extracted from pages (for example image `src`/`srcset` values).                                                                                                                                                                                                                  |

### Plan run directory layout

When **`PlanRunDirectory`** is set and **`NestRunUnderRoot`** is **`true`** (default), each invocation uses:

`{RootDirectory}/{RunFolderName or run id}/`

| Subdirectory     | Content                                                                                                                                                                                                                |
|------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`logs/`**      | UTF-8 **`run.log`** (or **`RunLogFileName`**) — UTC timestamp and tab-separated lines: **`RUN_STARTED`**, **`STEP_START`**, **`STEP_COMPLETE`**, **`STEP_FAILED`**, **`RUN_COMPLETED`** / **`RUN_END`**.               |
| **`snapshots/`** | Viewport PNGs: **`{stepIndex:000}_{stepExecutionId}_{before\|after\|failed}.png`** when **`WriteSnapshots`** and the corresponding timing flags are enabled.                                                           |
| **`variables/`** | JSON dumps of string / string-list variables (not element refs): **`step_{index:000}_after.json`**, **`step_{index:000}_failed.json`**, and **`final.json`** (or **`FinalVariablesFileName`**) on completion or fault. |

With **`NestRunUnderRoot`** = **`false`**, **`RootDirectory`** is the run root (same relative names for **`LogsSubdirectory`**, **`SnapshotsSubdirectory`**, *
*`VariablesSubdirectory`**); only one concurrent run should use the same path.

**`AutomationPlanRunDirectoryOptions`** controls each category independently:

| Property                               | Default        | Effect                                                   |
|----------------------------------------|----------------|----------------------------------------------------------|
| **`WriteRunLogFile`**                  | `true`         | Write `logs/run.log` transcript.                         |
| **`RunLogFileName`**                   | `"run.log"`    | File name inside `LogsSubdirectory`.                     |
| **`WriteSnapshots`**                   | `true`         | Master switch for PNG capture.                           |
| **`SnapshotBeforeEachStep`**           | `false`        | Capture before each step body (after `BeforeStepAsync`). |
| **`SnapshotAfterEachSuccessfulStep`**  | `true`         | Capture after each successful step.                      |
| **`SnapshotOnStepFailure`**            | `true`         | Capture when a step throws.                              |
| **`WriteVariables`**                   | `true`         | Master switch for variable JSON dumps.                   |
| **`VariablesAfterEachSuccessfulStep`** | `true`         | Write `step_{index:000}_after.json` after each success.  |
| **`VariablesOnStepFailure`**           | `true`         | Write `step_{index:000}_failed.json` when a step throws. |
| **`VariablesOnRunEnd`**                | `true`         | Write `final.json` on completion or fault (best-effort). |
| **`FinalVariablesFileName`**           | `"final.json"` | File name for the end-of-run variable dump.              |

Set all three master switches to **`false`** to reserve only the directory (useful when the directory itself is needed but file writes are not).

**Session directory fallback**: when **`PlanRunDirectory`** is `null` but the session was created via a browser service, the runner falls back to **`session.SessionDirectory`** (
with **`NestRunUnderRoot = false`**) so artifacts are written alongside the browser profile without any explicit configuration.

Artifact write failures are logged as warnings and do not fail the run (except invalid **`RootDirectory`** at start).

---

## Browser capabilities

### Cookie management

**`IWebAutomationBrowser.CookieJar`** exposes **`IBrowserCookies`** when the engine supports it (Playwright; `null` for Selenium). Call the `Try*` extension methods from *
*`WebAutomationBrowserExtensions`** for graceful degradation:

| Extension method                    | Behaviour when `CookieJar` is `null` |
|-------------------------------------|--------------------------------------|
| `TryGetCookiesAsync(url?, ct)`      | Returns empty list                   |
| `TryGetCookieHeaderAsync(url?, ct)` | Returns `null`                       |
| `TryAddCookiesAsync(cookies, ct)`   | No-op                                |
| `TryClearCookiesAsync(ct)`          | No-op                                |

**`BrowserCookie`** carries `Name`, `Value`, `Domain`, `Path`, `Secure`, `HttpOnly`, and `Expiry`.

```csharp
// Read cookies after login and format as a Cookie header
var cookieHeader = await session.Browser.TryGetCookieHeaderAsync(ct: ct);

// Inject cookies before navigation
await session.Browser.TryAddCookiesAsync([
    new BrowserCookie { Name = "session", Value = "abc123", Domain = "example.com" }
], ct);
```

### Extra request headers

**`IWebAutomationBrowser.ExtraHeaders`** exposes **`IBrowserHeaders`** when supported (Playwright). Headers are sent with every subsequent request:

| Extension method                       | Behaviour when `ExtraHeaders` is `null` |
|----------------------------------------|-----------------------------------------|
| `TrySetExtraHeadersAsync(headers, ct)` | No-op                                   |
| `TryClearExtraHeadersAsync(ct)`        | No-op                                   |

```csharp
await session.Browser.TrySetExtraHeadersAsync(
    new Dictionary<string, string> { ["Authorization"] = "Bearer token" }, ct);
```

### Navigation with request observation

**`NavigateAsync(url, onRequest, ct)`** overload on **`IWebAutomationNavigator`** calls `onRequest` with the URL of each outgoing network request observed before, during, and after
the page load. Return `true` from the callback to signal that the caller found what it needed (stops observation). For Chromium-based Selenium sessions, performance logging must be
enabled.

```csharp
string? apiUrl = null;
await session.Browser.NavigateAsync(
    "https://example.com/",
    req => {
        if (req.Contains("/api/data")) { apiUrl = req; return true; }
        return false;
    },
    ct);
```

### Page source and snapshots

| Member                                                        | Returns                                  |
|---------------------------------------------------------------|------------------------------------------|
| `IWebAutomationPage.GetPageSourceAsync(ct)` (also on browser) | Full HTML source of the current document |
| `IWebAutomationPage.TakeViewportSnapshotPngAsync(ct)`         | Visible viewport as a PNG byte array     |
| `IWebAutomationElement.TakeSnapshotPngAsync(ct)`              | Element bounding box as a PNG byte array |

These are also used internally by the runner when `WriteSnapshots` is enabled.

### Session directory

**`IWebAutomationSession.SessionDirectory`** is the per-session root (`{ServiceRootDirectory}/session-{SessionId:N}`). It contains:

- **`browser-profile/`** — browser user-data directory
- **`artifacts/`** — engine-specific downloads and outputs
- **`downloads/`** — files downloaded by the browser
- **plan run subdirectories** — when `PlanRunDirectory` is `null`, the runner writes logs, snapshots, and variables directly under this directory (see session directory fallback
  above)

`SessionDirectory` is `null` when the session was not created via a browser service.

---

## Logging utilities

### `SessionFileLoggerProvider`

**`SessionFileLoggerProvider`** is a per-session **`ILoggerProvider`** that appends structured log lines to `{sessionDirectory}/session.log` (ISO-8601 UTC timestamp, abbreviated
level, category, message). Create one per browser session and dispose it when the session ends:

```csharp
using var fileLoggerProvider = new SessionFileLoggerProvider(session.SessionDirectory!);
var sessionLogger = fileLoggerProvider.CreateLogger<MyWorker>();
```

### `CompositeLogger<T>`

**`CompositeLogger<T>`** fans log calls to two **`ILogger`** instances simultaneously — useful for writing to both the injected application logger and the session file logger
without changing call sites:

```csharp
var compositeLogger = new CompositeLogger<MyWorker>(appLogger, sessionFileLogger);
```

---

## Correlation ids and logging

Each **`RunWithResultAsync`** invocation gets a time-ordered **`automation_run_id`** (**`Guid.CreateVersion7()`** on .NET 9+; otherwise **`Guid.NewGuid()`**). The same value is
also on log scopes as **`plan_run_id`** for systems that prefer that name.

Every step execution gets a new time-ordered **`automation_step_execution_id`** (also **`plan_step_execution_id`** on scopes). Steps built via **`AutomationPlanBuilder`** carry a
stable definition **`StepId`**; scopes and message templates include **`plan_step_id`** and **`automation_plan_step_id`** when the id is set (omit when empty).

**Nested step work** (for example **`writeStringListToFile`** and **`downloadUrlsToDirectory`**) may run on the thread pool or otherwise without inheriting the ambient *
*`BeginScope`** from the outer step. The runner re-applies the same keys (**`plan_run_id`**, **`automation_step_execution_id`**, **`plan_step_id`**, **`automation_step`**, *
*`automation_step_index`**, …) around that work so structured logs stay aligned with the parent step.

The runner logs **start/complete** for the plan and each step with **duration** (ms) and **`StepOutcome`** (**`AutomationPlanStepOutcome`**: success, cancelled, timed out, or
failed). Pass **`ILogger`** for structured fields; when artifacts are configured (explicit **`PlanRunDirectory`** or session directory fallback), **`automation_plan_run_root`** is
added to log scopes and the run root is logged at start. **`IWebAutomationBrowser.TakeViewportSnapshotPngAsync`** and **`IWebAutomationElement.TakeSnapshotPngAsync`** are also
available for ad-hoc capture outside the run folder (see [Browser capabilities](#browser-capabilities)).

Implement **`IAutomationPlanInstrumentation`** for run/step lifecycle, or inherit **`AutomationPlanInstrumentationBase`** and override **`OnStepOutcome`** for a single hook per
finished step (**`AutomationPlanStepOutcomeRecord`**: duration, outcome, optional error) — suitable for histograms and outcome counters in OpenTelemetry or similar. *
*`OnStepFailed`** includes **`Outcome`** to distinguish cancellation vs step timeout vs other failures.

- **`AutomationPlanStepCompletedEvent`** is emitted only when a step finishes **without throwing** (duration only). Cancellation and failures are **`OnStepFailed`** / *
  *`OnStepOutcome`** instead — there is no redundant per-step “cancelled” flag on the completed event.
- **`AutomationPlanRunCompletedEvent`** carries **`AutomationPlanRunOutcome`**: **`Completed`** (normal return), **`Cancelled`** (**`OperationCanceledException`** from the caller
  token, plan timeout, or cooperative cancel), or **`Faulted`** (any other exception). Plan-level logs include **`RunOutcome`** for dashboards alongside step-level outcomes.

---

## Running a plan

1. Register the runner in DI: **`services.AddWebAutomationPlanRunner()`** (and your own sink/storage implementations when needed).
2. Resolve **`IAutomationPlanRunner`** plus an **`IWebAutomationSession`** from your host.
3. Call **`await runner.RunAsync(session, plan, logger, ct)`** — or **`RunWithResultAsync`** with optional **`AutomationPlanRuntimeOptions`**.

```csharp
using Lyo.Web.Automation.Plan;

services.AddWebAutomationPlanRunner();

var runner = provider.GetRequiredService<IAutomationPlanRunner>();
await runner.RunWithResultAsync(
    session,
    plan,
    new AutomationPlanRuntimeOptions {
        PlanRunDirectory = new AutomationPlanRunDirectoryOptions {
            RootDirectory = @"C:\data\automation-runs",
            // SnapshotBeforeEachStep = true,
            // WriteRunLogFile = false, WriteSnapshots = false, WriteVariables = false, // e.g. empty layout / future use
        },
    },
    logger,
    cancellationToken);
```

---

## Step reference (builder ↔ JSON `type`)

| Builder method                 | JSON `type`                                               | Notes                                                                                                                    |
|--------------------------------|-----------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| `Navigate`                     | `navigate`                                                | URL supports `{{vars}}`                                                                                                  |
| `Reload`                       | `reload`                                                  | Full document reload                                                                                                     |
| `Delay`                        | `delay`                                                   | Milliseconds                                                                                                             |
| `FindElement`                  | `findElement` (one segment) or `findElementChain` (multi) | Stores **element ref**                                                                                                   |
| `FindDescendant`             | `findDescendant`                                          | Polls **under a stored parent ref** (`PollForDescendantAsync`); locator `value` supports `{{vars}}`                      |
| `FindElements`                 | `findElementsChain`                                       | Stores **element list ref**                                                                                              |
| `ElementAction`                | `elementAction`                                           | Click, input, select, …                                                                                                  |
| `FindAndAct`                   | `findAndAct`                                              | Single locator                                                                                                           |
| `FindAndActChain`              | `findAndActChain`                                         | Chain                                                                                                                    |
| `ExtractElementData`           | `extractElementData`                                      | Text or attribute → string var                                                                                           |
| `ExtractElementsListData`      | `extractElementsListData`                                 | Per element → string list var                                                                                            |
| `StoreLiteral`                 | `storeLiteral`                                            | Value may contain `{{vars}}`                                                                                             |
| `StoreTemplate`                | `storeTemplate`                                           | Template → string var                                                                                                    |
| `StoreStringListFromTemplate`  | `storeStringListFromTemplate`                             | Map source list items through an item template                                                                           |
| `StorePageUrl`                 | `storePageUrl`                                            |                                                                                                                          |
| `StorePageTitle`               | `storePageTitle`                                          |                                                                                                                          |
| `WriteStringListToFile`        | `writeStringListToFile`                                   | UTF-8; path may use `{{vars}}`                                                                                           |
| `DownloadUrlsToDirectory`      | `downloadUrlsToDirectory`                                 | Uses runner HTTP dependency; optional **`urlListFromCompletedStepIndex`** (zero-based **completed** step index)          |
| `HttpRequest`                  | `httpRequest`                                             | Uses runner HTTP dependency; supports templated URL/headers/body and response capture                                    |
| `DownloadFile`                 | `downloadFile`                                            | Uses runner HTTP dependency; downloads one URL to one file                                                               |
| `ExtractSources`               | `extractSources`                                          | Extracts source/link attributes into list variable (configure attrs for video/CSV/etc.)                                  |
| `UpsertJsonRecords`            | `upsertJsonRecords`                                       | Uses runner data sink dependency; upserts JSON payload from a string variable                                            |
| `UploadDirectoryToFileStorage` | `uploadDirectoryToFileStorage`                            | Uses runner file storage dependency; uploads local directory and optionally stores uploaded keys/URLs                    |
| `InvokeDiMethod`               | `invokeDiMethod`                                          | Uses runner service provider dependency; resolves service by type and invokes method (supports context-aware signatures) |

**`ElementAction`** JSON uses nested **`type`**: `click`, `inputText`, `sendKeys`, `clear`, `submit`, `selectByText`, `selectByValue`, `selectByIndex`, `dropdown`.

### `dropdown` (native and custom)

Use a single **`dropdown`** action for both **`<select>`** and **custom** menus. The default **`mode`** is **`auto`**: the runner inspects the target element’s tag name—**`select`** uses native selection; anything else uses the custom path.

- **`mode`**: **`auto`** (default), **`native`**, or **`custom`**. With **`auto`**, you do not need to know the widget type in advance.
- **Native (`<select>`)**: set exactly one of **`selectByText`**, **`selectByValue`**, or **`selectByIndex`** (same semantics as the standalone `selectBy*` actions). With **`mode: auto`**, these fields are used when the target resolves to a `<select>`.
- **Custom widgets** (button opens a list, React Select, etc.): set **`optionLocator`** for one option row; do **not** set the native fields. With **`mode: auto`**, this applies when the target is not a `<select>`.
  - The step’s **element ref** is usually the **trigger** that opens the list.
  - **`optionLocator`**: often **`CssSelector`**, e.g. `[role=listbox] [data-value="x"]` or `#panel [role=option]:has-text("Beta")`.
  - **`clickTriggerFirst`** (default **`true`**): scroll into view and click the trigger before resolving the option.
  - **`scopeParentRef`**: optional stored ref for an **ancestor** of the options; the option is found with **`PollForDescendantAsync`** from that parent. If omitted, the option is resolved at **page scope** after the list opens (typical for portaled menus when a unique selector identifies the open list).

You can still use the standalone **`selectByText`** / **`selectByValue`** / **`selectByIndex`** actions on `<select>` elements, or compose **`click`** / **`findDescendant`** steps manually when **`dropdown`** is not enough.

---

## Examples

### 1. Fluent plan: search, extract, template

```csharp
using Lyo.Web.Automation;

var plan = AutomationPlanBuilder
    .New("Example search")
    .Navigate("https://example.com/")
    .FindAndAct(
        "q",
        ElementLocator.Name("q"),
        new InputTextElementAction("lyo automation", ClearFirst: true))
    .FindAndAct(
        "submit",
        ElementLocator.CssSelector("input[type=submit]"),
        new ClickElementAction())
    .Delay(500, "wait render")
    .StorePageUrl("resultUrl")
    .StoreTemplate("summary", "Opened: {{resultUrl}}")
    .Build();

// run with session + logger ...
```

### 2. `dropdown` on a custom widget (page-scoped option, `mode` defaults to auto)

```csharp
using Lyo.Web.Automation.Models;

var plan = AutomationPlanBuilder
    .New("Custom dropdown")
    .Navigate("https://example.com/form")
    .FindElement("menuTrigger", ElementLocator.Id("countryMenuBtn"))
    .ElementAction(
        "menuTrigger",
        new DropdownElementAction(
            OptionLocator: ElementLocator.CssSelector("#countryListbox [role=option][data-code=\"US\"]"),
            ClickTriggerFirst: true,
            ScopeParentRef: null))
    .Build();
```

### 3. Same, with option resolved under a stored parent ref

```csharp
.FindElement("panel", ElementLocator.Id("countryListbox")) // must be findable when hidden, or use a visible wrapper
.FindElement("menuTrigger", ElementLocator.Id("countryMenuBtn"))
.ElementAction(
    "menuTrigger",
    new DropdownElementAction(
        ElementLocator.CssSelector("[role=option][data-code=\"US\"]"),
        ClickTriggerFirst: true,
        ScopeParentRef: "panel"))
```

### 3b. `dropdown` on a native `<select>` (`auto` detects `select`)

```csharp
.ElementAction("countrySelect", new DropdownElementAction(SelectByValue: "US"))
```

### 4. Chained locators (nested scope)

```csharp
var chain = ElementLocator
    .CssSelector("#main")
    .Then(ElementLocator.CssSelector("article"))
    .Then(ElementLocator.CssSelector("h1"));

var plan = AutomationPlanBuilder
    .New()
    .Navigate("https://example.com/docs")
    .FindElement("heading", chain)
    .ExtractElementData("heading", "titleText", ElementDataExtractKind.Text)
    .Build();
```

### 5. List extraction and file output

```csharp
var listChain = new ElementLocatorChain(
    ElementLocator.CssSelector("ul.links"),
    ElementLocator.CssSelector("a"));

var plan = AutomationPlanBuilder
    .New()
    .Navigate("https://example.com/links")
    .FindElements("anchors", listChain)
    .ExtractElementsListData("anchors", "hrefs", ElementDataExtractKind.Attribute, attributeName: "href")
    .WriteStringListToFile("hrefs", "/tmp/hrefs.txt", append: false)
    .Build();
```

### 6. JSON plan (deserialize and run)

Types use **`System.Text.Json.Serialization`** attributes (`JsonPolymorphic` / `JsonDerivedType`) so **your** app can configure **`JsonSerializerOptions`** and deserialize. Example
shape (camelCase):

```json
{
  "name": "Login flow",
  "steps": [
    {
      "type": "navigate",
      "url": "https://example.com/login"
    },
    {
      "type": "findAndAct",
      "refName": "user",
      "locator": { "kind": "Id", "value": "username" },
      "action": { "type": "inputText", "text": "demo", "clearFirst": true }
    },
    {
      "type": "findAndAct",
      "refName": "pass",
      "locator": { "kind": "Id", "value": "password" },
      "action": { "type": "inputText", "text": "secret", "clearFirst": true }
    },
    {
      "type": "findAndAct",
      "refName": "go",
      "locator": { "kind": "CssSelector", "value": "button[type=submit]" },
      "action": { "type": "click", "scrollIntoView": true }
    }
  ]
}
```

```csharp
using System.Text.Json;

var json = await File.ReadAllTextAsync("plan.json", ct);
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var plan = JsonSerializer.Deserialize<AutomationPlan>(json, options)
    ?? throw new InvalidOperationException("Invalid plan JSON.");

await runner.RunAsync(session, plan, logger, ct);
```

### 7. Reading results (`RunWithResultAsync`)

```csharp
var result = await runner.RunWithResultAsync(
    session, plan, runtime: null, logger, ct);

// String tables only (no element refs)
var title = result.Snapshot.Strings["titleText"];

// Full bindings: elements + lists + strings (preferred for automation follow-up)
if (result.Context.Overall.TryGetElement("heading", out var heading))
{
    var text = await heading.GetTextAsync(ct);
}

// Historical: string list as it was after step index 3 (zero-based)
if (result.Context.TryGetStringListAtCompletedStep(3, "hrefs", out var hrefsAtStep3))
{
    // use hrefsAtStep3
}
```

### 8. Downloads from a URL list (with optional step snapshot)

After a step fills a **string list** variable (for example `srcs`), **`downloadUrlsToDirectory`** saves each URL to **`targetDirectory`** (runner must have an HTTP dependency
configured). By default the runner uses the **final** value of that variable. If a **later** step overwrites or clears it, set *
*`urlListFromCompletedStepIndex`** to the **zero-based completed step index** whose bindings should be used (the snapshot **after** that step finished).

In this mini-plan, steps are indices `0` = navigate, `1` = find elements, `2` = extract list → variable `srcs`. To download using `srcs` exactly as it was after the extract step,
pass **`urlListFromCompletedStepIndex: 2`**.

```csharp
var plan = AutomationPlanBuilder
    .New()
    .Navigate("https://example.com/gallery")
    .FindElements("imgs", ElementLocator.CssSelector("img"))
    .ExtractElementsListData("imgs", "srcs", ElementDataExtractKind.Attribute, attributeName: "src")
    .DownloadUrlsToDirectory(
        urlListVariableName: "srcs",
        targetDirectory: "/tmp/gallery",
        fileNamePrefix: "img",
        urlListFromCompletedStepIndex: 2)
    .Build();
```

### 9. Scrape fields, bind DTO JSON, and call an API

```csharp
var plan = AutomationPlanBuilder
    .New("Scrape and submit DTO")
    .Navigate("https://example.com/product/42")
    .FindElement("titleEl", ElementLocator.CssSelector("h1.product-title"))
    .ExtractElementData("titleEl", "productTitle", ElementDataExtractKind.Text)
    .StoreLiteral("productId", "42")
    .StoreTemplate("upsertProductDtoJson", "{\"productId\":\"{strings.productId}\",\"title\":\"{strings.productTitle}\"}")
    .HttpRequest(
        method: "POST",
        url: "https://api.myapp.local/products/upsert",
        headers: new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        bodyTemplate: "{strings.upsertProductDtoJson}",
        responseBodyVariableName: "apiResponseBody",
        statusCodeVariableName: "apiStatusCode")
    .Build();
```

### 10. Scrape file links, download, then upload to storage

```csharp
var plan = AutomationPlanBuilder
    .New("Download and upload assets")
    .Navigate("https://example.com/gallery/abc")
    .FindElements("downloadAnchors", new ElementLocatorChain(ElementLocator.CssSelector("a.download-link")))
    .ExtractElementsListData("downloadAnchors", "downloadUrls", ElementDataExtractKind.Attribute, attributeName: "href")
    .DownloadUrlsToDirectory("downloadUrls", "/tmp/gallery", "asset")
    .UploadDirectoryToFileStorage("/tmp/gallery", "gallery-assets/{page.title}", "uploadedFileUrls")
    .Build();
```

You can also target non-image sources by specifying attributes explicitly (for example video links or CSV URLs):

```csharp
var plan = AutomationPlanBuilder
    .New("Extract CSV links")
    .Navigate("https://example.com/exports")
    .FindElements("exportAnchors", new ElementLocatorChain(ElementLocator.CssSelector("a.export-link")))
    .ExtractSources(
        elementsListRefName: "exportAnchors",
        variableName: "csvUrls",
        attributeNames: new[] { "href" },
        splitCommaSeparatedValues: false)
    .DownloadUrlsToDirectory("csvUrls", "/tmp/exports", "csv")
    .Build();
```

### 11. Invoke DI scraper method and reuse context

```csharp
// DI registration in your host
services.AddScoped<MangaFireScraper>();

public sealed class MangaFireScraper(IApiClient apiClient)
{
    public async Task Scrape(AutomationPlanStepContext stepContext, CancellationToken ct)
    {
        // write context for later built-in steps
        stepContext.ContextItems["auth.cookie"] = "session=abc";
        await Task.CompletedTask;
    }
}

var plan = AutomationPlanBuilder
    .New("DI scrape hook")
    .InvokeDiMethod(
        serviceType: "MyApp.Scrapers.MangaFireScraper, MyApp",
        methodName: "Scrape")
    .StoreTemplate("cookieHeader", "{context.auth.cookie}")
    .Build();
```

---

## Target frameworks

All three projects (**`Lyo.Web.Automation`**, **`Lyo.Web.Automation.Selenium`**, **`Lyo.Web.Automation.Playwright`**) target **`netstandard2.0`** and **`net10.0`**.

---

## Design notes

- **Polling**: Find operations use the browser’s **`PollForElementAsync`** / **`PollForElementsAsync`** semantics (timeouts and behavior are defined by the concrete browser
  implementation).
- **Cancellation**: Pass **`CancellationToken`** through **`RunAsync` / `RunWithResultAsync`**; steps respect cooperative cancellation.
- **Logging**: Optional **`ILogger`**; the runner adds scopes for session and step labels (`step.Name` when set, otherwise the step type name).

For product-level API context, see `Lyo.Api` documentation where applicable; this README focuses on the **WebAutomation plan** library and **examples** above.
