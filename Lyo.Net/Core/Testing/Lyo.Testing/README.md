# Lyo.Testing

Helpers for xUnit v3: fluent `Should*` asserts, exception and collection helpers, asynchronous polling checks, and a logger that writes through `ITestOutputHelper`.

## Features

- **TestData.** Reproducible seeded byte payloads (`TestData.Seed` / `Create` / `Fill`). Shares its seed with `BenchmarkData.PayloadSeed` and `DeterministicPayloadStream.DefaultSeed`.

## Examples

### Payloads from a seed

```csharp
using Lyo.Testing;

var plaintext = TestData.Create(1024);
var wrongKey = TestData.Create(32, TestData.Seed ^ 1);
TestData.Fill(buffer);
```

### Wait-and-assert

```csharp
// Async — boolean predicate, value match, custom predicate, or no-exception loop.
await PollAssert.ThatAsync(() => queue.Count == 0, TimeSpan.FromSeconds(5));
await PollAssert.ThatAsync(() => provider.GetState(), expectedValue: "Ready", TimeSpan.FromSeconds(5));
await PollAssert.ThatAsync(() => provider.GetState(), s => s.IsHealthy, TimeSpan.FromSeconds(5),
                            failureMessage: "Provider never became healthy");
await PollAssert.NoExceptionAsync(() => probe.PingAsync(), TimeSpan.FromSeconds(5));

// Synchronous equivalents
PollAssert.That(() => queue.Count == 0, TimeSpan.FromSeconds(5));
PollAssert.That(() => provider.GetState(), expectedValue: "Ready", TimeSpan.FromSeconds(5));
PollAssert.That(() => provider.GetState(), s => s.IsHealthy, TimeSpan.FromSeconds(5));
PollAssert.NoException(() => probe.Ping(), TimeSpan.FromSeconds(5));
```

## Asserts

- **Comparing values.** `ShouldBe(expected)`, `ShouldNotBe(unexpected)`, `ShouldBeSameAs`, `ShouldNotBeSameAs`, `ShouldBeAssignableTo<T>()`, `ShouldBeOfType<T>()`.
- **Nulls.** `ShouldBeNull` / `ShouldNotBeNull` (overloads for reference and nullable-value types. `[NotNull]` flows from the `NotNull` variants for nullable-flow analysis).
- **Booleans.** `ShouldBeFalse(message?)`, `ShouldBeTrue(message?)`.
- **Ordering / ranges (where `T : IComparable<T>`).** `ShouldBeLessThan`, `ShouldBeLessThanOrEqualTo`, `ShouldBeGreaterThan`, `ShouldBeGreaterThanOrEqualTo`, `ShouldBeBetween`.
- **Time.** `TimeSpan.ShouldBeCloseTo(expected, tolerance)`, `DateTime.ShouldBeCloseTo(expected, tolerance)`.
- **Strings.** `ShouldEndWith`, `ShouldStartWith`, `ShouldContain(substring)`, `ShouldNotContain`, `ShouldMatch(pattern)`, `ShouldNotMatch`, `ShouldBeEmpty`, `ShouldNotBeEmpty`.
- **Collections.** `ShouldNotBeEmpty`, `ShouldBeEmpty`, `ShouldContain(item)`, `ShouldNotContain`, `ShouldContainAll(expected)`, `ShouldContainNone(excluded)`, `ShouldHaveCount(count)`, `ShouldHaveCount(count, predicate)`, `ShouldAllSatisfy(predicate)`, `ShouldAnySatisfy(predicate)`, `ShouldHaveUniqueItems`, `ShouldBeEquivalentTo(expected)`, plus `ShouldBeOrdered` / `ShouldBeOrderedDescending` for `IComparable<T>` collections.

## Exceptions

- `Throws<T>(action, message?)` / `ThrowsAsync<T>(func, message?)`
- `ThrowsAny(action, params Type[])` / `ThrowsAnyAsync(func, params Type[])`. Succeeds when `GetType()` of the captured exception is in the supplied list.
- `ThrowsWithInnerException<T>(action, innerType)` / `ThrowsWithInnerExceptionAsync<T>(func, innerType)`. Checks both the outer and inner exception types.
- `DoesNotThrow(action)` / `DoesNotThrowAsync(func)`.

## Wait-and-assert

`Lyo.Testing.PollAssert` wraps Polly retry and timeout policies for wait-until-true checks.

Every overload takes an optional `pollInterval` (defaults to `100 ms`) and, on the predicate variants, an optional `failureMessage`.

## Logging

`XunitLoggerProvider(ITestOutputHelper output)` is an `ILoggerProvider` that forwards `ILogger` calls to `ITestOutputHelper.WriteLine` with `HH:mm:ss.fff` timestamps and `TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL|NONE` level tags. Wire it via `services.AddLogging(b => b.AddProvider(new XunitLoggerProvider(output)))` when using `Xunit.DependencyInjection`. Writes that fail against the output helper are swallowed (the runner can already be torn down when the last logs flush).

## Utilities

`Lyo.Testing.Utilities.AppendBytesToFile(string path, long sizeInBytes)` grows (or creates) a file by the requested number of bytes and creates parent directories if needed. Handy for filesystem tests that care about size.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Streams` (direct, lyo)
- `Microsoft.NET.Test.Sdk` `18` (direct, microsoft)
- `Polly` `8.7.0` (direct, third-party)
- `Xunit.DependencyInjection` `11.3.0` (direct, third-party)
- `coverlet.collector` `10.0.1` (direct, third-party)
- `xunit.runner.visualstudio` `3.1.5` (direct, third-party)
- `xunit.v3` `3.2.2` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)