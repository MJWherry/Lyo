# Lyo.Webhook.Twilio

Twilio webhook signature validation for `Lyo.Webhook`. Compares `X-Twilio-Signature` to an HMAC-SHA1 (Base64) of the public request URL plus sorted key+value form parameters, matching Twilio's server-side behavior (including URL variants with or without an explicit default port).

Reference: [Twilio webhooks security](https://www.twilio.com/docs/usage/webhooks/webhooks-security).

## Examples

### Usage

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

## Usage

- Use the `Lyo.Webhook` fluent pipeline so the body is read once, `RequestUrl` is set, and `Parameters` are filled for `application/x-www-form-urlencoded` posts.
- Ensure `WebhookVerificationContext.RequestUrl` matches the URL Twilio called (scheme, host, path, query). The default `GetPublicRequestUrl()` helper uses the current request. Behind reverse proxies, configure forwarded headers / public base URL so this matches Twilio's URL.
- For form webhooks, `Parameters` must contain all form fields Twilio sends. The core library populates `Parameters` when `Content-Type` is `application/x-www-form-urlencoded`.

## Types

- **TwilioWebhookSignatureVerifier.** Implements `IWebhookSignatureVerifier`. Constructor takes the Twilio Auth Token.
- **TwilioUrlNormalization.** Internal URL variants (explicit `:443` / `:80` vs default) used in signature comparison.

## Target framework

- **net10.0**

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Webhook` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)