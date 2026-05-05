# Lyo.Formatter

**SmartFormat.NET**-backed templating for user-defined strings: named placeholders, lists, pluralization, and culture-aware formatting. Designed for **validation + formatting** pipelines (for example **`IFormatterService`** with **`Lyo.Api`** computed fields and **`Lyo.Web.Automation`** step templates).

## When to use this package

- Turn stored templates (`"{User.Name} — {Order.Total:C}"`) into final text with one or more context objects.
- **Validate** templates before persisting them (`ValidateTemplate`, `TryValidateTemplate`).
- Discover **placeholders** for dependency analysis (`GetPlaceholders`, `GetUnresolvedPlaceholders`, `AllPlaceholdersResolved`).
- Build context **fluently** with **`IContextBuilder`** (dates, conditional keys, custom formatters).

## Registration

```csharp
using Lyo.Formatter;
using Microsoft.Extensions.DependencyInjection;

services.AddFormatterService();
// Or: services.AddFormatterService(sp => /* custom SmartFormatter */);
```

Register **`FormatterService`** as singleton and expose **`IFormatterService`**. Prefer the overload with a factory when you need extra SmartFormat extensions or custom **`SmartSettings`**.

## Core types

| Type | Role |
|------|------|
| **`IFormatterService`** | Format, validate, inspect placeholders, wrap templates as **`ITemplate`**. |
| **`FormatterService`** | Default implementation: **`FormatErrorAction.MaintainTokens`** so missing data leaves `{tokens}` in output (enables unresolved-placeholder detection). Case-insensitive placeholder matching. |
| **`ITemplate`** | Parse-once style workflow: **`WithContext`**, **`AddContext`**, **`TryValidateContext`**, then **`Format()`**. |
| **`IContextBuilder`** | Fluent dictionary builder passed to **`Format(template, configure)`**. |

## Formatting overloads

- **`Format(template, object? context)`** — single DTO, anonymous object, or any type SmartFormat can reflect over.
- **`Format(template, params object?[] contextItems)`** — multiple sources; later objects win on duplicate names.
- **`Format(template, IReadOnlyDictionary<string, object?>)`** — explicit name/value map.
- **`Format(template, Action<IContextBuilder>)`** — build the map with **`Add`**, **`AddIf`**, **`AddWhen`**, typed format strings, or custom **`Func<,>`** formatters.

**Culture:** set **`Culture`** on the service; when null, **`CultureInfo.CurrentCulture`** is used.

## Validation and placeholders

- **`ValidateTemplate` / `TryValidateTemplate`** — parser pass; catches syntax errors before you save a template.
- **`TryFormat`** — swallows exceptions from SmartFormat and returns false (use sparingly; prefer validation + known context).
- **`GetPlaceholders`** — lightweight regex-based names (first segment of each `{...}`); good for UI hints and **`entityTypes`**-style dependency lists.
- **`AllPlaceholdersResolved` / `GetUnresolvedPlaceholders`** — compare template to formatted output; relies on **`MaintainTokens`** so missing keys stay visible as `{Name}`.

## `ITemplate` workflow

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

Use **`AddContext`** on the template to layer **`IContextBuilder`** steps without allocating a full dictionary at the call site.

## SmartFormat behavior

This library does **not** fork SmartFormat; it configures a **`SmartFormatter`** instance. Refer to the [SmartFormat documentation](https://github.com/axuno/SmartFormat/wiki) for list formatting, plural rules, and built-in extensions.

**Automation note:** **`Lyo.Web.Automation`** documents that step templates use **single-brace** placeholders (`{page.url}`); legacy `{{page.url}}` is normalized there. This service accepts standard SmartFormat templates as-is.

## Integration points

- **`Lyo.Api`** — optional **`IFormatterService`** for **`ComputedFields`** on projection/query responses (SmartFormat templates over projected rows).
- **`Lyo.Web.Automation`** — optional **`IFormatterService`** to validate automation plans before execution.

## Thread safety

**`FormatterService`** is safe for concurrent reads if you do not mutate **`SmartFormatter`** or **`Culture`** from multiple threads without synchronization. Typical ASP.NET Core registration as a singleton treats **`Culture`** as ambient per request by setting it at the start of a request (or avoid mutating **`Culture`** on the shared instance and pass culture-aware data in context instead).

## Related projects

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) (computed fields)
- [`Lyo.Web.Automation`](../../../Integration/Web/Automation/Lyo.Web.Automation/README.md) (template validation)

---

## Dependencies

*(From `Lyo.Formatter.csproj`.)*

**Target frameworks:** `net10.0`, `netstandard2.0`

### NuGet packages

| Package | Version |
|---------|---------|
| `SmartFormat.NET` | `[3.6,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
