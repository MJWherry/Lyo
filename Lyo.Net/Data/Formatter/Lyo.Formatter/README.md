# Lyo.Formatter

SmartFormat.NET templating for user-defined strings: named placeholders, lists, pluralization, and culture-aware formatting. Built for validate-then-format pipelines. `IFormatterService` is what `Lyo.Api` computed fields and `Lyo.Web.Automation` step templates call.

## Examples

### Register services

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

## When to use this package

- Turn stored templates (`"{User.Name} {Order.Total:C}"`) into final text with one or more context objects.
- Validate templates before persisting them (`ValidateTemplate`, `TryValidateTemplate`).
- List placeholders for dependency analysis (`GetPlaceholders`, `GetUnresolvedPlaceholders`, `AllPlaceholdersResolved`).
- Build context with `IContextBuilder` (dates, conditional keys, custom formatters).

## Registration

Register `FormatterService` as a singleton and expose `IFormatterService`. Use the factory overload when you need extra SmartFormat extensions or custom `SmartSettings`.

## Core types

| Type | Role |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IFormatterService` | Format, validate, inspect placeholders, wrap templates as `ITemplate`, emit annotated `FormatSegments`. |
| `FormatterService` | Default implementation. Uses `FormatErrorAction.MaintainTokens` so missing data leaves `{tokens}` in output, which is how unresolved-placeholder detection works. Placeholder matching is case-insensitive. |
| `ITemplate` | Parse-once workflow: `WithContext`, `AddContext`, `TryValidateContext`, then `Format()`. |
| `IContextBuilder` | Dictionary builder passed to `Format(template, configure)`. |
| `FormatterSegment` / `FormatterSegmentKind` | Annotated span from `FormatSegments`: literal text, a resolved replacement, or an unresolved `{token}`, plus the placeholder key and raw template substring. |

## Formatting overloads

- `Format(template, object? context)`. One DTO, anonymous object, or any type SmartFormat can reflect over.
- `Format(template, params object?[] contextItems)`. Multiple sources. Later objects win on duplicate names.
- `Format(template, IReadOnlyDictionary<string, object?>)`. Explicit name/value map.
- `Format(template, Action<IContextBuilder>)`. Build the map with `Add`, `AddIf`, `AddWhen`, typed format strings, or custom `Func<,>` formatters.

## Validation and placeholders

- `ValidateTemplate` / `TryValidateTemplate`. Parser pass. Catches syntax errors before you save a template.
- `TryFormat`. Swallows exceptions from SmartFormat and returns false. Prefer validation plus known context.
- `GetPlaceholders`. Regex-based names (first segment of each `{...}`). Useful for UI hints and `entityTypes`-style dependency lists.
- `AllPlaceholdersResolved` / `GetUnresolvedPlaceholders`. Compare template to formatted output. Relies on `MaintainTokens` so missing keys stay visible as `{Name}`.
- `FormatSegments`. Walks the parsed template into ordered `FormatterSegment` spans (literal / placeholder / unresolved) so UIs can color-link `{Name}` to its replacement without a second parser.

## `ITemplate` workflow

Use `AddContext` on the template to layer `IContextBuilder` steps without allocating a full dictionary at the call site. `Format(additionalContext)` merges a one-off
context (dictionary or object) on top of the accumulated state for a single render.

`ITemplate.TryValidateContext` succeeds when the accumulated context keys cover every placeholder name (or supply a parent path like `Order` for `{Order.Total}`). Bare CLR objects
passed via `WithContext(object?)` do not participate in this check (only the merged dictionary and dictionary-shaped extras do), so call `WithValue`/`AddContext` or supply a
dictionary when you want the validator to confirm coverage.

## SmartFormat behavior

This library does not fork SmartFormat. It configures a `SmartFormatter` instance. See the [SmartFormat documentation](https://github.com/axuno/SmartFormat/wiki) for list formatting, plural rules, and built-in extensions. `Lyo.Web.Automation` step templates use single-brace placeholders (`{page.url}`). Legacy `{{page.url}}` is normalized there. This service accepts standard SmartFormat templates as-is.

## Integration points

- `Lyo.Api`. Optional `IFormatterService` for `ComputedFields` on projection/query responses (SmartFormat templates over projected rows).
- `Lyo.Web.Automation`. Optional `IFormatterService` to validate automation plans before execution.
- `Lyo.Formatter.Web.Components`. Live template editor and annotated preview (`FormatSegments`). Works on WASM.

## Thread safety

`FormatterService` is safe for concurrent reads if you do not mutate `SmartFormatter` or `Culture` from multiple threads without synchronization. Typical ASP.NET Core registration as a singleton treats `Culture` as ambient per request by setting it at the start of a request. Or leave `Culture` alone on the shared instance and pass culture-aware data in context instead.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `SmartFormat.NET` `3.6.1` (direct, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)