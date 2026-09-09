# Lyo.Webhook.Twilio

Twilio webhook signature checks for `Lyo.Webhook`. Compares `X-Twilio-Signature` to an HMAC-SHA1 (Base64) of the public request URL plus sorted key+value form parameters, matching Twilio's server-side behavior (including URL variants with or without an explicit default port).

Reference: [Twilio webhooks security](https://www.twilio.com/docs/usage/webhooks/webhooks-security).

## Examples

### How to use it

```csharp
var authToken = configuration["Twilio:AuthToken"]!;
var verifier = new TwilioWebhookSignatureVerifier(authToken);

app.MapWebhook("/webhooks/twilio/sms")
    .Verify(verifier)
    .Handle(async ctx =>
    {
        // ctx.Body contains the form body; verification already succeeded
        await ctx.HttpContext.Response.WriteAsync("OK");
    });
```

## How to use it

- Go through the `Lyo.Webhook` fluent pipeline so the body is read once, `RequestUrl` is assigned, and `Parameters` are populated for `application/x-www-form-urlencoded` posts.
- Make sure `WebhookVerificationContext.RequestUrl` matches the URL Twilio called (scheme, host, path, query). The default `GetPublicRequestUrl()` helper uses the current request. Behind reverse proxies, configure forwarded headers / public base URL so this matches Twilio's URL.
- For form webhooks, `Parameters` must contain every form field Twilio sends. The core library fills `Parameters` when `Content-Type` is `application/x-www-form-urlencoded`.

## Types

- **TwilioWebhookSignatureVerifier.** An `IWebhookSignatureVerifier`. Pass the Twilio Auth Token to the constructor.
- **TwilioUrlNormalization.** Signature comparison uses internal URL variants (explicit `:443` / `:80` versus the default).

## Framework target

- **net10.0**

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Webhook` (direct, lyo)
- `Lyo.Metrics` (transitive, lyo)