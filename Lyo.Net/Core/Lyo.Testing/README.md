# Lyo.Testing

Part of the [Lyo](../../../README.md) workspace: shared **xUnit v3** helpers for the rest of the solution — fluent `Should*` assertions, exception/collection helpers,
polling-based asynchronous assertions, and an `ITestOutputHelper`-backed logger.

## Assertions

`Lyo.Testing.Extensions` is a `static class` with `extension(...)` blocks and old-style extension methods that wrap Xunit `Assert.*`:

- **Value comparison** — `ShouldBe(expected)`, `ShouldNotBe(unexpected)`, `ShouldBeSameAs`, `ShouldNotBeSameAs`, `ShouldBeAssignableTo<T>()`, `ShouldBeOfType<T>()`.
- **Nulls** — `ShouldBeNull` / `ShouldNotBeNull` (overloads for reference and nullable-value types; the `NotNull` variants flow `[NotNull]` for nullable-flow analysis).
- **Booleans** — `ShouldBeTrue(message?)`, `ShouldBeFalse(message?)`.
- **Ordering / ranges (where `T : IComparable<T>`)** — `ShouldBeGreaterThan`, `ShouldBeGreaterThanOrEqualTo`, `ShouldBeLessThan`, `ShouldBeLessThanOrEqualTo`, `ShouldBeBetween`.
- **Time** — `DateTime.ShouldBeCloseTo(expected, tolerance)`, `TimeSpan.ShouldBeCloseTo(expected, tolerance)`.
- **Strings** — `ShouldStartWith`, `ShouldEndWith`, `ShouldContain(substring)`, `ShouldNotContain`, `ShouldMatch(pattern)`, `ShouldNotMatch`, `ShouldBeEmpty`, `ShouldNotBeEmpty`.
- **Collections** — `ShouldBeEmpty`, `ShouldNotBeEmpty`, `ShouldContain(item)`, `ShouldNotContain`, `ShouldContainAll(expected)`, `ShouldContainNone(excluded)`,
  `ShouldHaveCount(count)`, `ShouldHaveCount(count, predicate)`, `ShouldAllSatisfy(predicate)`, `ShouldAnySatisfy(predicate)`, `ShouldHaveUniqueItems`,
  `ShouldBeEquivalentTo(expected)`, plus `ShouldBeOrdered` / `ShouldBeOrderedDescending` for `IComparable<T>` collections.

The static **`CollectionAssertions`** class exposes the underlying helpers (`ContainsAll`, `ContainsNone`, `ContainsExactly`, `IsEmpty`, `IsNotEmpty`, `AllSatisfy`,
`AnySatisfies`, `IsOrdered`, `IsOrderedDescending`, `HasUniqueItems`) if you prefer non-fluent calls.

## Exceptions

`Lyo.Testing.ExceptionAssertions` wraps Xunit's exception assertions with an optional substring check on `Exception.Message`:

- `Throws<T>(action, message?)` / `ThrowsAsync<T>(func, message?)`
- `ThrowsAny(action, params Type[])` / `ThrowsAnyAsync(func, params Type[])` — passes when the captured exception's `GetType()` is in the supplied list.
- `ThrowsWithInnerException<T>(action, innerType)` / `ThrowsWithInnerExceptionAsync<T>(func, innerType)` — asserts both the outer and inner exception types.
- `DoesNotThrow(action)` / `DoesNotThrowAsync(func)`.

## Polling

`Lyo.Testing.PollAssert` wraps **Polly** retry + timeout policies for "wait until eventually true" scenarios:

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

All overloads accept an optional `pollInterval` (defaults to `100 ms`) and an optional `failureMessage` on the predicate variants.

## Logging

`XunitLoggerProvider(ITestOutputHelper output)` is an `ILoggerProvider` that forwards `ILogger` calls to `ITestOutputHelper.WriteLine` with `HH:mm:ss.fff` timestamps and
`TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL|NONE` level tags. Wire it up via `services.AddLogging(b => b.AddProvider(new XunitLoggerProvider(output)))` when using
`Xunit.DependencyInjection`. Errors writing to the output helper are swallowed (the runner can be torn down before final log flushes).

## Utilities

`Lyo.Testing.Utilities.AppendBytesToFile(string path, long sizeInBytes)` grows (or creates) a file by the requested number of bytes, creating parent directories as needed. Useful
for size-based filesystem tests.

## Dependencies

*(Synchronized from `Lyo.Testing.csproj`.)*

**Target framework:** `net10.0` (`IsPackable=false` — consumed via project reference only)

### NuGet packages

| Package                     | Version |
|-----------------------------|---------|
| `coverlet.collector`        | `8.0.1` |
| `Microsoft.NET.Test.Sdk`    | `[18,)` |
| `Polly`                     | `8.*`   |
| `xunit.v3`                  | `3.2.2` |
| `xunit.runner.visualstudio` | `3.1.5` |
| `Xunit.DependencyInjection` | `11.*`  |

### Project references

- [`Lyo.Exceptions`](../Lyo.Exceptions/README.md)
- [`Lyo.IO.Temp`](../../Data/IOTemp/Lyo.IO.Temp/README.md)
