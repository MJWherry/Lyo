# Lyo.Common.Json

The JSON contract Lyo APIs, clients, and message-queue envelopes serialize against lives in `Lyo.Common.Json`: camelCase properties, case-insensitive reads, nulls omitted on write, camelCase string enums, and `ReferenceHandler.IgnoreCycles` so EF-shaped graphs serialize without blowing up.

Converters under `JsonConverters/` stay **unregistered** by default. They exist for vendor payloads that quote numbers or spell booleans as `0` / `1`; add them per integration through `LyoJsonSerializerOptionsBuilder`.

[`Lyo.Common.Metadata`](../Lyo.Common.Metadata/README.md) is intentionally not referenced. The two converters that need registry records ship there, which keeps both packages acyclic. Namespaces match the package: options are `Lyo.Common.Json.LyoJsonSerializerOptions` and converters live in `Lyo.Common.Json.JsonConverters`.

## Features

- **`LyoJsonSerializerOptions`.** `Create()` returns a fresh options instance — safe to hand to DI or mutate further — and `Create(configure)` applies a delegate to it. `ApplyTo(target)` copies the defaults onto an existing instance for ASP.NET Core's `ConfigureHttpJsonOptions`, which requires in-place mutation, and skips converters already present.
- **`LyoJsonSerializerOptionsBuilder`.** Fluent composition over the defaults, or over a supplied baseline: `AddConverter`, `WithWriteIndented`, `WithDefaultIgnoreCondition`, then `Build()` for a copy.
- **`ObjectJsonConverter`.** Opt-in STJ adapter over `TypeConversion.FromJsonElement`: `object` properties and collection items deserialize as CLR primitives (string, number, bool, list) instead of `JsonElement`.
- **String-wrapped booleans.** `StringIntBoolConverter` and its nullable counterpart accept real JSON booleans, numbers, and quoted spellings alike.
- **`StringEnumConverter<TEnum>`.** Enum round-tripping for a specific enum type, for the cases where the suite-wide `JsonStringEnumConverter` naming policy is not what the upstream sends.
- **`StringDateTimeConverter`.** Constructed with the exact formats an integration uses (`params string[] formats`), for upstreams whose timestamps are not round-trippable.

## Examples

### Serialize against the shared contract

```csharp
using Lyo.Common.Json;

var options = LyoJsonSerializerOptions.Create();
var json = JsonSerializer.Serialize(dto, options);
var back = JsonSerializer.Deserialize<MyDto>(json, options);
```

### Apply the defaults to ASP.NET Core

```csharp
using Lyo.Common.Json;

// ConfigureHttpJsonOptions hands you the live instance, so copy onto it rather than replacing it.
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
```

### A variant for one integration

```csharp
using Lyo.Common.Json;
using Lyo.Common.Json.JsonConverters;

// This upstream quotes its numbers and sends booleans as 0/1.
var options = new LyoJsonSerializerOptionsBuilder()
    .AddConverter(new StringIntConverter())
    .AddConverter(new StringIntBoolConverter())
    .AddConverter(new StringDateTimeConverter("yyyyMMdd", "yyyy-MM-dd HH:mm"))
    .Build();
```

## Why record converters live in the other package

`SocialPlatformInfoJsonConverter` and `LanguageCodeInfoJsonConverter` resolve a code to a registry record, so they would drag `Lyo.Common.Metadata` in behind them. Keeping them on the metadata side means a project that only wants the scalar converters does not acquire the catalogs, and neither package references the other.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)