namespace Lyo.Idempotency.AspNetCore;

/// <summary>
/// Requires an <c>Idempotency-Key</c> header on the endpoint. Safe methods ignore it.
/// Put it on a controller, an action, or a minimal-API endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RequireIdempotencyKeyAttribute : Attribute;
