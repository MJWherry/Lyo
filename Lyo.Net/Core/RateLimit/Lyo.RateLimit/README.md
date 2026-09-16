# Lyo.RateLimit

TryAcquireAsync takes one permit or returns a denial with Retry-After. The store does not wait. Use it only where a caller already has a quota. AddRedisRateLimit replaces this registration when more than one host must share that quota.

## Features

- **IRateLimitService.** Non-blocking acquire by policy, partition, limit, window, and pattern.
- **Fixed window.** Clock-aligned count. A full limit can be spent again at the next boundary.
- **Sliding window.** Weighted previous window plus the current window, rounded up.
- **Token bucket.** Continuous refill from the last update, capped at the permit limit.
- **Whitelist matching.** RateLimitEndpointMatcher skips named endpoints and method/route templates before a store call.
- **DI.** AddLocalRateLimit, the options instance overload, and AddLocalRateLimitFromConfiguration. TryAdd leaves a Redis registration in place.

## Examples

### One process

```csharp
services.AddLocalRateLimit();

var decision = await limiter.TryAcquireAsync(
    new RateLimitRequest("uploads", userId, 10, TimeSpan.FromMinutes(1), RateLimitWindow.Sliding));
if (!decision.Allowed)
    throw new RateLimitExceededException(decision.RetryAfter, 10, TimeSpan.FromMinutes(1));
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)