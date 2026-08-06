# Lyo.Webhook

Inbound webhook verification for ASP.NET Core: **raw body + headers**, **HMAC helpers**, a **fluent `MapWebhook().Verify().Handle()`** pipeline, **`Lyo.Metrics` timings and counters**, and **structured logging** via `Microsoft.Extensions.Logging`.

Provider-specific algorithms (e.g. Twilio) live in separate packages such as **Lyo.Webhook.Twilio**.

## Features

- **Abstractions**: `IWebhookSignatureVerifier`, `WebhookVerificationContext`, `WebhookVerificationResult`
- **Crypto helpers**: `WebhookCrypto` (HMAC-SHA256 / SHA1, constant-time compare, hex parse)
- **ASP.NET Core**: read raw body, header dictionary, public URL, optional **form-urlencoded** parameters for signed form posts
- **Fluent routes**: `MapWebhook("/path").Verify(verifier).Handle(...)` or `HandleJson<T>(...)`
- **Metrics** (`Lyo.Metrics`): `lyo.webhook.request.duration`, `lyo.webhook.verification.duration`, `lyo.webhook.handler.duration`, verification success/failure counters, JSON parse failures, handler errors
- **Logging**: category **`Lyo.Webhook`** (debug for incoming requests, warning on failed verification / bad JSON, error on handler exceptions)

## Examples

### Register services

```csharp
services.AddLyoMetrics();
// logging: AddLogging(), etc.
```

### Fluent mapping

```csharp
app.MapWebhook("/webhooks/example")
    .Verify(myVerifier)
    .Handle(async ctx =>
    {
        // ctx.Body is verified; ctx.HttpContext.Response...
    })
    .WithName("ExampleWebhook");

app.MapWebhook("/webhooks/json-example")
    .Verify(myVerifier)
    .HandleJson<MyPayload>(async ctx =>
    {
        var payload = ctx.Request;
    });
```

## Registration

Register **`Lyo.Metrics`** and logging in your host (same as other Lyo services): At runtime the webhook pipeline resolves **`IMetrics`** and **`ILoggerFactory`** from **`HttpContext.RequestServices`**. If **`IMetrics`** is missing, **`NullMetrics`** is used; if **`ILoggerFactory`** is missing, **`NullLogger`** is used.

## Fluent mapping

- Failed signature → **401**
- Invalid JSON (when using `HandleJson`) → **400**
- Metric tag **`route`** = route pattern string (keep cardinality low)

## Manual verification (no fluent API)

Use `WebhookCrypto`, `WebhookHeaders`, and `HttpRequest` extensions (`ReadRawBodyAsync`, `ToWebhookHeaderDictionary`, `GetPublicRequestUrl`) to build a `WebhookVerificationContext` and call your `IWebhookSignatureVerifier` directly.

## Target framework

- **net10.0** only (same line as the rest of this solution).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)