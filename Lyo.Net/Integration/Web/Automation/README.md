# Lyo.Web.Automation

**Serializable automation plans** and a shared **runner** for **Selenium** and **Playwright** via `IWebAutomationSession` / `IWebAutomationBrowser`.

| Project | Role |
| --- | --- |
| `Lyo.Web.Automation` | Plan types, `AutomationPlanBuilder`, `AutomationPlanRunner`, interpolation, hooks / metrics |
| `Lyo.Web.Automation.Selenium` | Selenium-backed session and `LyoBrowser` |
| `Lyo.Web.Automation.Playwright` | Playwright-backed session and browser adapter |

Plans are **ordered lists of steps** (`AutomationPlan`). Steps can navigate, find elements (single or lists), act on elements, extract text or attributes into **string variables**, write files, download URL lists, and store literals or page metadata. String values can be combined with **`{{variableName}}`** placeholders resolved at run time.

---

## Concepts

### `AutomationPlan`

- **`Name`**: Optional label for logging and persistence.
- **`Steps`**: **`IReadOnlyList<AutomationStepDefinition>`** — immutable after **`AutomationPlanBuilder.Build()`** (defensive copy). Each step is a polymorphic record; if you serialize to JSON yourself, a typical shape discriminates on **`type`**.

Build in code with **`AutomationPlanBuilder`** (assigns a time-ordered **`StepId`** on every step when you did not set one). Deserialize with **`System.Text.Json`** (or anything else) in your host — the library does not ship a serializer.

### Locators and chains

- **`ElementLocator`**: One strategy (`ElementLocatorKind`: `Id`, `CssSelector`, `XPath`, …) plus a **`Value`**.
- **`ElementLocatorChain`**: One or more segments, outer → inner (nested find / chained Playwright locator). Construct with `new ElementLocatorChain(a, b)` or fluently: `ElementLocator.CssSelector("#app").Then(ElementLocator.CssSelector("button"))`.

Single-segment chains are equivalent to a simple find; multi-segment steps use chain-specific step types in JSON (`findElementChain`, `findElementsChain`, …).

### Variables and bindings

During a run, the engine maintains:

| Binding | Meaning |
| --- | --- |
| **Element refs** | Named `IWebAutomationElement` instances from find steps |
| **Element list refs** | Named lists from `findElementsChain` |
| **String variables** | From extract / store / page URL / title steps |
| **String list variables** | From list extract steps |

**Interpolation** (`AutomationPlanInterpolation.ExpandAsync` during a run): templates in navigate URLs, store steps, element actions, file paths, etc. resolve against **live bindings** — the same information you see later in **`AutomationPlanBindings`** / **`Context.Overall`**, not only pre-stored string variables. Optional **`AutomationPlanRuntimeOptions.Formatter`** (**`Lyo.Formatter.IFormatterService`**) validates the template with SmartFormat (same engine as **`FormatterService`**) before resolution.

| Form | Meaning |
| --- | --- |
| `{name}` or `{{name}}` | String variable `name` (legacy double braces are normalized to single). Same as `strings.name`. |
| `{strings.x}` / `{str.x}` | String variable `x`. |
| `{lists.x}` / `{list.x}` | String-list variable `x`, lines joined with newlines. |
| `{page.url}`, `{page.title}` | Current document URL / title (live from the browser). |
| `{elements.ref.text}` or `{el.ref.text}` | Visible text of element ref `ref`. |
| `{elements.ref.attr.href}` | Attribute on element ref `ref`. |

If the entire selector matches a string variable key (including keys with dots), that value is used first. A SmartFormat format specifier after `:` is ignored for resolution (only the part before `:` is used as the selector). Legacy synchronous **`Expand`** only supports simple `{{name}}` from a string dictionary (for callers outside the runner).

Avoid stray `{…}` in templates except for placeholders—anything between `{` and `}` is treated as a selector.

### Execution results

**`AutomationPlanRunner.RunWithResultAsync`** returns **`AutomationPlanRunResult`**:

- **`Snapshot`**: Final **`Strings`** and **`StringLists`** only (no element handles) — convenient for logging or APIs.
- **`Context`**: Full picture:
  - **`Context.Overall`**: **`AutomationPlanBindings`** after the **last** step — **preferred** for reading final element refs and all variables.
  - **`Context.Frames`**: One **`AutomationPlanStepFrame`** per completed step (historical snapshots). Use when you need state **as it was after step *n*** (for example, comparing a URL list before a later step overwrote it).

Frame index **`i`** is state **after** `plan.Steps[i]` completed (zero-based).

---

## Runtime options

**`AutomationPlanRuntimeOptions`** is **not** part of the serialized plan. Pass it to **`RunWithResultAsync`** when needed:

| Property | Use |
| --- | --- |
| **`HttpClient`** | Required for **`downloadUrlsToDirectory`** (each URL is fetched and saved under the target directory). |
| **`DownloadFileNamePrefix`** | Default file name prefix when a step does not set one (runner default is `download`). |
| **`PlanTimeout`** | Optional ceiling for the **entire** run (combined with the run `CancellationToken`). |
| **`DefaultStepTimeout`** | Optional default per-step limit; a step can override with **`AutomationStepDefinition.StepTimeout`**. |
| **`Hooks`** | **`BeforeStepAsync`**, **`AfterStepAsync`**, **`OnFailureAsync`** (`AutomationPlanHooks`). |
| **`Instrumentation`** | Optional **`IAutomationPlanInstrumentation`** for metrics / tracing (run and step lifecycle). |
| **`Formatter`** | Optional **`Lyo.Formatter.IFormatterService`**: validates step templates with SmartFormat before placeholders are resolved. Use single-brace placeholders (e.g. `{page.url}`) or legacy `{{page.url}}` (normalized to single braces). Register **`FormatterService`** from DI if you use this. |

---

## Correlation ids and logging

Each **`RunWithResultAsync`** invocation gets a time-ordered **`automation_run_id`** (**`Guid.CreateVersion7()`** on .NET 9+; otherwise **`Guid.NewGuid()`**). The same value is also on log scopes as **`plan_run_id`** for systems that prefer that name.

Every step execution gets a new time-ordered **`automation_step_execution_id`** (also **`plan_step_execution_id`** on scopes). Steps built via **`AutomationPlanBuilder`** carry a stable definition **`StepId`**; scopes and message templates include **`plan_step_id`** and **`automation_plan_step_id`** when the id is set (omit when empty).

**Nested step work** (for example **`writeStringListToFile`** and **`downloadUrlsToDirectory`**) may run on the thread pool or otherwise without inheriting the ambient **`BeginScope`** from the outer step. The runner re-applies the same keys (**`plan_run_id`**, **`automation_step_execution_id`**, **`plan_step_id`**, **`automation_step`**, **`automation_step_index`**, …) around that work so structured logs stay aligned with the parent step.

The runner logs **start/complete** for the plan and each step with **duration** (ms) and **`StepOutcome`** (**`AutomationPlanStepOutcome`**: success, cancelled, timed out, or failed). Pass **`ILogger`** for structured fields.

Implement **`IAutomationPlanInstrumentation`** for run/step lifecycle, or inherit **`AutomationPlanInstrumentationBase`** and override **`OnStepOutcome`** for a single hook per finished step (**`AutomationPlanStepOutcomeRecord`**: duration, outcome, optional error) — suitable for histograms and outcome counters in OpenTelemetry or similar. **`OnStepFailed`** includes **`Outcome`** to distinguish cancellation vs step timeout vs other failures.

- **`AutomationPlanStepCompletedEvent`** is emitted only when a step finishes **without throwing** (duration only). Cancellation and failures are **`OnStepFailed`** / **`OnStepOutcome`** instead — there is no redundant per-step “cancelled” flag on the completed event.
- **`AutomationPlanRunCompletedEvent`** carries **`AutomationPlanRunOutcome`**: **`Completed`** (normal return), **`Cancelled`** (**`OperationCanceledException`** from the caller token, plan timeout, or cooperative cancel), or **`Faulted`** (any other exception). Plan-level logs include **`RunOutcome`** for dashboards alongside step-level outcomes.

---

## Running a plan

1. Obtain an **`IWebAutomationSession`** from your app’s Selenium or Playwright service.
2. Call **`await session.StartBrowserAsync(ct)`** (the runner also calls this, but your host may start earlier).
3. **`await AutomationPlanRunner.RunAsync(session, plan, logger, ct)`** — or **`RunWithResultAsync`** with optional **`AutomationPlanRuntimeOptions`**.

```csharp
using Lyo.Web.Automation;

await AutomationPlanRunner.RunWithResultAsync(
    session,
    plan,
    new AutomationPlanRuntimeOptions { HttpClient = httpClient },
    logger,
    cancellationToken);
```

---

## Step reference (builder ↔ JSON `type`)

| Builder method | JSON `type` | Notes |
| --- | --- | --- |
| `Navigate` | `navigate` | URL supports `{{vars}}` |
| `Reload` | `reload` | Full document reload |
| `Delay` | `delay` | Milliseconds |
| `FindElement` | `findElement` (one segment) or `findElementChain` (multi) | Stores **element ref** |
| `FindElements` | `findElementsChain` | Stores **element list ref** |
| `ElementAction` | `elementAction` | Click, input, select, … |
| `FindAndAct` | `findAndAct` | Single locator |
| `FindAndActChain` | `findAndActChain` | Chain |
| `ExtractElementData` | `extractElementData` | Text or attribute → string var |
| `ExtractElementsListData` | `extractElementsListData` | Per element → string list var |
| `StoreLiteral` | `storeLiteral` | Value may contain `{{vars}}` |
| `StoreTemplate` | `storeTemplate` | Template → string var |
| `StorePageUrl` | `storePageUrl` | |
| `StorePageTitle` | `storePageTitle` | |
| `WriteStringListToFile` | `writeStringListToFile` | UTF-8; path may use `{{vars}}` |
| `DownloadUrlsToDirectory` | `downloadUrlsToDirectory` | Needs `HttpClient`; optional **`urlListFromCompletedStepIndex`** (zero-based **completed** step index) |

**`ElementAction`** JSON uses nested **`type`**: `click`, `inputText`, `sendKeys`, `clear`, `submit`, `selectByText`, `selectByValue`, `selectByIndex`.

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

### 2. Chained locators (nested scope)

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

### 3. List extraction and file output

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

### 4. JSON plan (deserialize and run)

Types use **`System.Text.Json.Serialization`** attributes (`JsonPolymorphic` / `JsonDerivedType`) so **your** app can configure **`JsonSerializerOptions`** and deserialize. Example shape (camelCase):

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

await AutomationPlanRunner.RunAsync(session, plan, logger, ct);
```

### 5. Reading results (`RunWithResultAsync`)

```csharp
var result = await AutomationPlanRunner.RunWithResultAsync(
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

### 6. Downloads from a URL list (with optional step snapshot)

After a step fills a **string list** variable (for example `srcs`), **`downloadUrlsToDirectory`** saves each URL to **`targetDirectory`** (requires **`AutomationPlanRuntimeOptions.HttpClient`**). By default the runner uses the **final** value of that variable. If a **later** step overwrites or clears it, set **`urlListFromCompletedStepIndex`** to the **zero-based completed step index** whose bindings should be used (the snapshot **after** that step finished).

In this mini-plan, steps are indices `0` = navigate, `1` = find elements, `2` = extract list → variable `srcs`. To download using `srcs` exactly as it was after the extract step, pass **`urlListFromCompletedStepIndex: 2`**.

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

---

## Target frameworks

All three projects (**`Lyo.Web.Automation`**, **`Lyo.Web.Automation.Selenium`**, **`Lyo.Web.Automation.Playwright`**) target **`netstandard2.0`** and **`net10.0`**.

---

## Design notes

- **Polling**: Find operations use the browser’s **`PollForElementAsync`** / **`PollForElementsAsync`** semantics (timeouts and behavior are defined by the concrete browser implementation).
- **Cancellation**: Pass **`CancellationToken`** through **`RunAsync` / `RunWithResultAsync`**; steps respect cooperative cancellation.
- **Logging**: Optional **`ILogger`**; the runner adds scopes for session and step labels (`step.Name` when set, otherwise the step type name).

For product-level API context, see `Lyo.Api` documentation where applicable; this README focuses on the **WebAutomation plan** library and **examples** above.
