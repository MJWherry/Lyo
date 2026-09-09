# Lyo.Parameters

A named parameter with a declared type, an optional allowed-value list, and a runtime value supplied as a string is what jobs, reports, and query templates all describe. That model used to live in two places — the keyed-value contracts in `Lyo.Common.Core/Parameters/` and the definition model in `Lyo.Query.Models/Parameters/` — which meant every consumer imported both namespaces and `Lyo.Query.Models` was the de facto owner of a concept that has nothing to do with querying.

This package is that model in one place, under one namespace, depending on neither persistence nor the query stack. The three types that genuinely need a query request (`ParameterOptionsJson`, `ParameterOptions`, `ParameterOptionsBinder`) stayed behind in `Lyo.Query.Models`.

## Features

- **Keyed values.** `LyoKeyedValueExtensions` and `ILyoKeyedValue` for reading typed values out of string-keyed parameter bags.
- **Definitions.** `LyoParameterSpec` — type, name, requiredness, default, and allowed values.
- **Allowed-value lists.** `ParameterListJson`, `ParameterOptionsItem`, `ParameterListJsonKind`, `ParameterOptionsKind` (Static, Query, Sproc).
- **Validation.** `LyoParameterValidator` checks supplied values against their spec.
- **Default channel.** `LyoParameterDefaults` and `LyoParameterDefaultKind` keep "what type is this value" separate from "how is its default written", so a `System.DateTime` parameter can default to an expression and still be edited and validated as a date.
- **JSON coercion.** `LyoParameterValueJson` converts loose text into the JSON a declared type accepts, so the validator and the resolver agree by construction.
- **Mapping.** `LyoParameterMapper` moves between runtime values and definitions.

## Examples

### Read typed values from a parameter bag

```csharp
using Lyo.Parameters;

var asOf = parameters.GetValue<DateOnly?>("AsOfDate");
var includeVoid = parameters.GetValue<bool>("IncludeVoided");
```

### A DateTime parameter whose default is yesterday

```csharp
using Lyo.Parameters;

// Type still describes the value; the default is authored separately.
var spec = new LyoParameterSpec(
    "AsOfDate",
    "System.DateTime",
    DefaultKind: LyoParameterDefaultKind.Expression,
    DefaultTemplate: "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}");

// resolver comes from DI: AddFormatterService registers one over IFormatterService.
if (LyoParameterDefaults.TryResolve(spec, literalValue: null, resolver, out var value, out var error))
    request.Parameters.Add(new(spec.Key, spec.Type, value));
else
    errors.Add(error!);
```

## Package contents

- `LyoKeyedValueExtensions`, `ILyoKeyedValue` — the keyed-value contract and typed accessors.
- `LyoParameterValidator`, `LyoParameterSpec`, `LyoParameterMapper` — the definition model and its checks.
- `LyoParameterDefaults`, `LyoParameterDefaultKind`, `LyoParameterValueJson`, `LyoTemplateResolver` — the default channel and the JSON coercion both it and validation rely on.
- `ParameterOptionsKind`, `ParameterOptionsItem`, `ParameterListJsonKind`, `ParameterListJson` — allowed-value lists and their JSON shapes.

## Kinds of defaults

- `Literal` — the default is the value stored on the definition, already valid for the declared type. This is the historical behaviour and the value every existing row reads as.
- `Expression` — the default is the template in `DefaultTemplate`, rendered when no value is supplied. The rendered text is normalized to the declared type and then validated like any other value, so validation stays type-strict.
- Rendered text is coerced to the declared type, not just quoted: a template renders through `ToString()` with whatever format specifier the author wrote, so `{DateTime.Now}` yields the culture spelling (`8/26/2026 10:33:14 AM`) rather than ISO, and `{Count:N0}` on an int yields `1,234`. `LyoParameterValueJson.Normalize` parses those — including group separators and a currency symbol — and re-serializes canonically, so display formatting never changes what type the value is.
- Coercion is about spelling, never about the type. Fractional text on an integer parameter (`{Total:N2}` on an `Int32`) is rejected rather than truncated, percent formatting (`{Rate:P}` renders `12.34 %` for a stored `0.1234`) is rejected rather than stored a hundred times too large, and text that is no date at all still fails a `DateTime`.
- Rendering needs a `LyoTemplateResolver`, which Core declares but does not implement: `Lyo.Formatter`'s `AddFormatterService` registers one over SmartFormat. A host without a formatter keeps working — only a parameter that actually declares an expression default fails, and it fails naming the missing registration.
- `LyoParameterValidator.ValidateSpec` rejects a half-set channel (a template on a Literal, or Expression with no template), so bad metadata fails on save rather than on every run that needs the default.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)