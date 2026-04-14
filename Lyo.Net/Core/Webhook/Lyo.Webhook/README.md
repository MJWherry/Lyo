# Lyo.Webhook

Inbound webhook verification for ASP.NET Core: **raw body + headers**, **HMAC helpers**, a **fluent `MapWebhook().Verify().Handle()`** pipeline, **`Lyo.Metrics` timings and
counters**, and **structured logging** via `Microsoft.Extensions.Logging`.

Provider-specific algorithms (e.g. Twilio) live in separate packages such as **Lyo.Webhook.Twilio**.

## Features

- **Abstractions**: `IWebhookSignatureVerifier`, `WebhookVerificationContext`, `WebhookVerificationResult`
- **Crypto helpers**: `WebhookCrypto` (HMAC-SHA256 / SHA1, constant-time compare, hex parse)
- **ASP.NET Core**: read raw body, header dictionary, public URL, optional **form-urlencoded** parameters for signed form posts
- **Fluent routes**: `MapWebhook("/path").Verify(verifier).Handle(...)` or `HandleJson<T>(...)`
- **Metrics** (`Lyo.Metrics`): `lyo.webhook.request.duration`, `lyo.webhook.verification.duration`, `lyo.webhook.handler.duration`, verification success/failure counters, JSON
  parse failures, handler errors
- **Logging**: category **`Lyo.Webhook`** (debug for incoming requests, warning on failed verification / bad JSON, error on handler exceptions)

## Registration

Register **`Lyo.Metrics`** and logging in your host (same as other Lyo services):

```csharp
services.AddLyoMetrics();
// logging: AddLogging(), etc.
```

At runtime the webhook pipeline resolves **`IMetrics`** and **`ILoggerFactory`** from **`HttpContext.RequestServices`**. If **`IMetrics`** is missing, **`NullMetrics`** is used; if
**`ILoggerFactory`** is missing, **`NullLogger`** is used.

## Fluent mapping

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

- Failed signature → **401**
- Invalid JSON (when using `HandleJson`) → **400**
- Metric tag **`route`** = route pattern string (keep cardinality low)

## Manual verification (no fluent API)

Use `WebhookCrypto`, `WebhookHeaders`, and `HttpRequest` extensions (`ReadRawBodyAsync`, `ToWebhookHeaderDictionary`, `GetPublicRequestUrl`) to build a `WebhookVerificationContext`
and call your `IWebhookSignatureVerifier` directly.

## Target framework

- **net10.0** only (same line as the rest of this solution).

## Related packages

- **Lyo.Webhook.Twilio** — Twilio `X-Twilio-Signature` validation

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Webhook.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*15*). Nested types and file-scoped namespaces may omit some entries.

- `HttpRequestWebhookExtensions`
- `IWebhookSignatureVerifier`
- `IWebhookVerificationOptions`
- `VerifiedWebhookEndpointBuilder`
- `WebhookCrypto`
- `WebhookEndpointMappingBuilder`
- `WebhookEndpointRouteBuilderExtensions`
- `WebhookHandlerContext`
- `WebhookHeaders`
- `WebhookInstrumentation`
- `WebhookMetrics`
- `WebhookRequestContext`
- `WebhookVerificationContext`
- `WebhookVerificationFailureReason`
- `WebhookVerificationResult`

<!-- LYO_README_SYNC:END -->

