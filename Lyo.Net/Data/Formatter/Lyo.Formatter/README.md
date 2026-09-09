# Lyo.Formatter

SmartFormat.NET templates for user-defined strings: named placeholders, lists, pluralization, and culture-aware formatting, plus a C# subset for DateTime math, ternary, and in-memory LINQ. Built for validate-then-format pipelines. `IFormatterService` is what `Lyo.Api` computed fields, `Lyo.Job.Worker` string parameters, and `Lyo.Web.Automation` step templates call.

## Examples

### Add FormatterService

```csharp
using Lyo.Formatter;
using Microsoft.Extensions.DependencyInjection;

services.AddFormatterService();
// Or: services.AddFormatterService(sp => /* custom SmartFormatter */);
```

### `ITemplate` workflow

```csharp
var t = formatter.CreateTemplate("{Title} — {Count}")
    .WithValue("Title", doc.Title)
    .WithValue("Count", doc.Count);

if (!t.TryValidate(out var err))
    throw new InvalidOperationException(err);

if (!t.TryValidateContext(out var ctxErr))
    throw new InvalidOperationException(ctxErr);

var text = t.Format();
```

## Who this package is for

- Render stored templates (`"{User.Name} {Order.Total:C}"`) into final text from one or more context objects.
- Validate templates before they are persisted (`ValidateTemplate`, `TryValidateTemplate`).
- List placeholders for dependency analysis via `GetPlaceholders`, `GetUnresolvedPlaceholders`, `AllPlaceholdersResolved`.
- Build context through `IContextBuilder` (dates, conditional keys, custom formatters).

## How to register

Register `FormatterService` as a singleton and expose `IFormatterService`. Use the factory overload when you need extra SmartFormat extensions or custom `SmartSettings`. `AddFormatterService` also registers `FormatterLyoType.Template` (`Lyo.Formatter.Template`, `IsClr` false) on the `LyoTypeInfo` catalog so job/report pickers can select formatter templates, and the `LyoTemplateResolver` that job and report services use to resolve expression parameter defaults. Call `AddParameterTemplateResolver` directly only when wiring `IFormatterService` by hand.

## Default expression parameters

A job or report parameter can declare `DefaultKind = Expression` and put a template in `DefaultTemplate`, which is how a `System.DateTime` parameter defaults to yesterday without giving up its type. Rendering happens in `Lyo.Parameters` through the `LyoTemplateResolver` registered here. Core owns the policy; this package owns the SmartFormat. Use `FormatterParameterDefaults.TryValidate` from write validators and editor previews so the message an author reads is the one that would have blocked the save.

## Core types

| Type | Role |
| ------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IFormatterService` | Format, validate, inspect placeholders, wrap templates as `ITemplate`, and emit annotated `FormatSegments`. |
| `FormatterService` | Default implementation. Uses `FormatErrorAction.MaintainTokens` so missing data leaves `{tokens}` in output, which is how unresolved-placeholder detection works. Placeholder matching ignores case. |
| `ITemplate` | Parse-once workflow: `WithContext`, `AddContext`, `TryValidateContext`, then `Format()`. |
| `IContextBuilder` | Dictionary builder passed to `Format(template, configure)`. |
| `FormatterSegment` / `FormatterSegmentKind` | Annotated span from `FormatSegments`: literal text, a resolved replacement, or an unresolved `{token}`, plus the placeholder key and the raw template substring. |
| `FormatterLyoType` | Non-CLR catalog type `Lyo.Formatter.Template` (`IsClr` false, editor kind Formatter). Stored values are JSON strings such as `"{DateTime.UtcNow}"` or `"{-}"`. `IsFormattable` / `IsFormattableName` are the job/report helpers (CLR string or formatter editor kind), not on `LyoTypeInfo`. |
| `FormatterParameterDefaults` | Bridges SmartFormat to `Lyo.Parameters`: `CreateResolver` produces the `LyoTemplateResolver` that expression parameter defaults resolve through, and `TryValidate` checks a default template against the parameter's declared type (syntax, that it renders, and that what it renders is a valid `System.DateTime`, `System.Int32`, …). |

## Formatting overloads

- `Format(template, object? context)`. One DTO, anonymous object, or any type SmartFormat can reflect.
- `Format(template, params object?[] contextItems)`. Multiple sources. Later objects win when names collide.
- `Format(template, IReadOnlyDictionary<string, object?>)`. An explicit name/value map.
- `Format(template, Action<IContextBuilder>)`. Build the map with `Add`, `AddIf`, `AddWhen`, typed format strings, or custom `Func<,>` formatters.

## Validate templates and placeholders

- `ValidateTemplate` / `TryValidateTemplate`. Parser pass. Catches syntax errors before a template is saved. Unknown context identifiers are not syntax errors.
- `TryFormat`. Swallows exceptions from SmartFormat and returns false. Prefer validation plus a known context.
- `GetPlaceholders`. Selector paths and expression member paths the host must supply (`amount`, `lastSuccessJobRun.Timestamp`). Drops type names (`DateTime`) and lambda parameters.
- `AllPlaceholdersResolved` / `GetUnresolvedPlaceholders`. Compare the template to formatted output. Relies on `MaintainTokens` so missing keys stay visible as `{Name}`.
- `FormatSegments`. Walks the parsed template into ordered `FormatterSegment` spans (literal / placeholder / unresolved) so UIs can color-link `{Name}` to its replacement without a second parser.

## `ITemplate` workflow

Use `AddContext` on the template to layer `IContextBuilder` steps without allocating a full dictionary at the call site. `Format(additionalContext)` merges a one-off
context (dictionary or object) on top of the accumulated state for a single render.

`ITemplate.TryValidateContext` succeeds when the accumulated context keys cover every placeholder name (or supply a parent path like `Order` for `{Order.Total}`). Bare CLR objects
passed via `WithContext(object?)` do not participate in this check (only the merged dictionary and dictionary-shaped extras do), so call `WithValue`/`AddContext` or supply a
dictionary when you want the validator to confirm coverage.

## SmartFormat behavior

This library does not fork SmartFormat. Simple selectors (`{Name}`, `{Count:N0}`, `{Items:list:{}|, }`) still go through a configured `SmartFormatter`. See the [SmartFormat documentation](https://github.com/axuno/SmartFormat/wiki) for list formatting, plural rules, and built-in extensions. `{{` / `}}` emit a literal `{` / `}`; they are not a second placeholder form. Write `{Name}`, not `{{Name}}`. `Lyo.Web.Automation` step templates use single-brace placeholders (`{page.url}`). Legacy `{{page.url}}` is normalized there.

## Expressions

Tokens that are not a plain SmartFormat selector (ternary `? :`, `=>`, comparisons, arithmetic, `this.`, `DateTime` / `TimeSpan` / `Math` / `Convert` / LINQ) are evaluated with DynamicExpresso against the same context bag. Missing data and unknown methods leave the raw `{...}` (`MaintainTokens`). `TryValidateTemplate` returns syntax errors for the editor; unknown context names (`Order` in `{Order.Total > 6}`) are not syntax errors.

**Clock (injectable `Func<DateTimeOffset>` on `FormatterService`, default real time):** `{DateTime.Now}`, `{DateTime.UtcNow}`, `{DateTime.Today}`, `{DateTimeOffset.Now}` / `UtcNow`. Offset: `{DateTime.Now.AddDays(-1)}`, `{DateTime.UtcNow - TimeSpan.FromHours(24)}`. Format the result with a SmartFormat spec: `{DateTime.Now.AddDays(-1):yyyy-MM-dd}`.

**Context:** `{amount}`, `{this.amount}`, `{client.contact.emailAddress}`. Nested `{DateTime.UtcNow - {lastSuccessJobRun.Timestamp}}` is rewritten innermost-first to keep DateTime types.

**Logic / strings:** `{this.amount > 2 ? "true" : "false"}`, `{this.nickname ?? this.name}`, `{string.IsNullOrWhiteSpace(this.nickname) ? this.name : this.nickname}`, `{string.Join(", ", this.items.Select(x => x.Name))}`, `{this.items[0].Name}`, `{Convert.ToInt32(this.qty)}`.

**LINQ (in-memory `Enumerable` only):** `Where` / `Select` / `Count` / `Any` / `Sum` / `First` / `OrderBy` / `Take` / `ToList`, and the rest of the usual in-memory operators. Not IQueryable, not SQL.

Not in this pass: assignment, `new` of arbitrary types, extra assemblies, `{client.Delete()}`, `currentTimestamp` / `24hrs` aliases (use `DateTime.*` / `TimeSpan.*`).

## WASM

Default Blazor WASM (IL interpreted, no AOT) can run expressions: DynamicExpresso interprets expression trees and does not emit an assembly. Live preview in `Lyo.Formatter.Web.Components` stays in-process. WASM AOT and Native AOT cannot run the expression engine (DynamicExpresso has no AOT support). SmartFormat-only placeholders still work there; a failed expression eval leaves the `{token}`.

## Integration points

- `Lyo.Api`. Optional `IFormatterService` for `ComputedFields` on projection/query responses (SmartFormat and expressions over projected rows).
- `Lyo.Job.Worker`. Optional `IFormatterService` for in-memory string parameter placeholders (`{jobrun.parameters.startdate}`, `{client.contact.emailAddress}`, `{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}`).
- `Lyo.Web.Automation`. Optional `IFormatterService` to validate automation plans before they run.
- `Lyo.Formatter.Web.Components`. Live template editor and annotated preview (`FormatSegments`). Runs on WASM.

## Thread safety

`FormatterService` is safe for concurrent reads if you do not mutate `SmartFormatter` or `Culture` from multiple threads without synchronization. Typical ASP.NET Core registration as a singleton treats `Culture` as ambient per request by setting it at the start of a request. Or leave `Culture` alone on the shared instance and pass culture-aware data in context instead.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Parameters` (direct, lyo)
- `DynamicExpresso.Core` `2.19.3` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `SmartFormat.NET` `3.6.1` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)