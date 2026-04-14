# Lyo.Webhook.Twilio

**Twilio** webhook signature validation for **`Lyo.Webhook`**: compares **`X-Twilio-Signature`** to an **HMAC-SHA1** (Base64) of the public request URL plus sorted **key+value**
form parameters, matching Twilio’s server-side behavior (including URL variants with/without an explicit default port).

Reference: [Twilio — Webhooks security](https://www.twilio.com/docs/usage/webhooks/webhooks-security).

## Usage

1. Use the **`Lyo.Webhook`** fluent pipeline so the body is read once, **`RequestUrl`** is set, and **`Parameters`** are filled for **`application/x-www-form-urlencoded`** posts.

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

2. Ensure **`WebhookVerificationContext.RequestUrl`** matches the URL Twilio called (scheme, host, path, query). The default **`GetPublicRequestUrl()`** helper uses the current
   request; behind reverse proxies, configure forwarded headers / public base URL so this matches Twilio’s URL.

3. For **form** webhooks, **`Parameters`** must contain all form fields Twilio sends. The core library populates **`Parameters`** when **`Content-Type`** is *
   *`application/x-www-form-urlencoded`**.

## Types

- **`TwilioWebhookSignatureVerifier`** — implements **`IWebhookSignatureVerifier`**; constructor takes the Twilio **Auth Token**.
- **`TwilioUrlNormalization`** — internal URL variants (explicit **:443** / **:80** vs default) used in signature comparison.

## Target framework

- **net10.0**

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Webhook.Twilio.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Webhook`

## Public API (generated)

Top-level `public` types in `*.cs` (*2*). Nested types and file-scoped namespaces may omit some entries.

- `TwilioUrlNormalization`
- `TwilioWebhookSignatureVerifier`

<!-- LYO_README_SYNC:END -->

