# Lyo.RateLimit.Redis

AddRedisRateLimit replaces an in-memory IRateLimitService. Use it only when more than one host must share a quota. A missing Redis connection string fails registration instead of
falling back to memory.

## Features

- **Same three windows.** Fixed, sliding, and token bucket use one Lua script each.
- **One key.** The hash tag is {policy:partition}, so a sliding window never touches two slots.
- **Scripted clock.** Production passes Redis TIME. Tests pass unix milliseconds.
- **Fail closed.** RedisException and RedisTimeoutException deny the call unless FailOpen is set. A failure does not write.
- **DI.** AddRedisRateLimit, the connection-string overload, and AddRedisRateLimitFromConfiguration.

## Examples

### Shared quota

```csharp
services.AddRedisRateLimit(redisConnectionString);
services.AddFlaredHttpClientFromConfiguration(configuration);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.RateLimit` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `StackExchange.Redis` `2.12.0` (direct, third-party)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)