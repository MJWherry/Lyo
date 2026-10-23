using Lyo.Exceptions;

namespace Lyo.Idempotency;

/// <summary>Shared checks for scope, key, hash, and expiry.</summary>
internal static class IdempotencyRules
{
    /// <summary>Reserved prefix. <see cref="IIdempotencyStore.TryBeginAsync" /> rejects it. Outbox inserts use it.</summary>
    public const string OutboxPrefix = "outbox/";

    public static void ThrowIfApiScope(string scope, string key, byte[] requestHash, IdempotencyOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ThrowIfScopeAndKey(scope, key, options);
        if (scope.StartsWith(OutboxPrefix, StringComparison.Ordinal))
            throw new ArgumentException("Scopes that start with 'outbox/' are reserved.", nameof(scope));

        ThrowIfHash(requestHash);
    }

    public static void ThrowIfScopeAndKey(string scope, string key, IdempotencyOptions options)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scope);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (scope.Length > options.MaxScopeLength)
            throw new ArgumentException($"Scope length {scope.Length} exceeds {options.MaxScopeLength}.", nameof(scope));

        if (key.Length > options.MaxKeyLength)
            throw new ArgumentException($"Key length {key.Length} exceeds {options.MaxKeyLength}.", nameof(key));
    }

    public static void ThrowIfHash(byte[] requestHash)
    {
        ArgumentHelpers.ThrowIfNull(requestHash);
        if (requestHash.Length == 0)
            throw new ArgumentException("Request hash is required.", nameof(requestHash));

        if (requestHash.Length != 32)
            throw new ArgumentException("Request hash must be SHA-256 (32 bytes).", nameof(requestHash));
    }

    public static DateTime ResolveExpiry(DateTime? expiresAt, DateTime utcNow, TimeSpan lifetime)
    {
        var now = AsUtc(utcNow, nameof(utcNow));
        if (expiresAt is null)
            return now.Add(lifetime);

        return AsUtc(expiresAt.Value, nameof(expiresAt));
    }

    public static DateTime AsUtc(DateTime value, string name)
    {
        if (value.Kind == DateTimeKind.Local)
            throw new ArgumentException("Local DateTime is not accepted. Pass UTC or Unspecified.", name);

        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static bool IsTimeExpired(DateTime? expiresAt, DateTime utcNow) => expiresAt is DateTime exp && exp <= utcNow;
}