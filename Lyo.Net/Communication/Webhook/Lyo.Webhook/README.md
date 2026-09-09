# Lyo.Webhook

ASP.NET Core inbound webhook verification: headers and raw body, HMAC helpers, a fluent `MapWebhook().Verify().Handle()` pipeline, `Lyo.Metrics` counters and timings, and structured logging via `Microsoft.Extensions.Logging`.

Algorithms that are provider-specific (e.g. Twilio) live in separate packages such as `Lyo.Webhook.Twilio`.

## Features

- **Abstractions.** `WebhookVerificationContext`, `IWebhookSignatureVerifier`, `WebhookVerificationResult`.
- **Crypto.** `WebhookCrypto` (SHA1 / HMAC-SHA256, hex parse, constant-time compare).
- **ASP.NET Core.** Raw body, header dictionary, public URL, optional form-urlencoded parameters for signed form posts.
- **Fluent routes.** `MapWebhook("/path").Verify(verifier).Handle(...)` or `HandleJson<T>(...)`.
- **Metrics.** `lyo.webhook.verification.duration`, `lyo.webhook.request.duration`, `lyo.webhook.handler.duration`, verification success/failure counters, JSON parse failures, handler errors.
- **Logging.** Category `Lyo.Webhook` (incoming requests at debug, failed verification / bad JSON at warning, handler exceptions at error).

## Examples

### Register in DI

```csharp
services.AddLyoMetrics();
// logging: AddLogging(), etc.
```

### Fluent route mapping

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

Register logging and `Lyo.Metrics` in your host. At runtime the webhook pipeline resolves `ILoggerFactory` and `IMetrics` from `HttpContext.RequestServices`. Missing `IMetrics` becomes `NullMetrics`. Missing `ILoggerFactory` becomes `NullLogger`.

## Fluent route mapping

- 401 on a failed signature
- 400 on invalid JSON (when using `HandleJson`)
- The `route` metric tag is the route pattern string (keep cardinality low)

## Verify by hand (no fluent API)

Build a `WebhookVerificationContext` from `WebhookCrypto`, `WebhookHeaders`, and `HttpRequest` extensions (`ToWebhookHeaderDictionary`, `ReadRawBodyAsync`, `GetPublicRequestUrl`) and call your `IWebhookSignatureVerifier` directly.

## Framework target

- Only net10.0.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)