# Lyo.Authentication.Web.Components.Server

Blazor Server host adapter for [`Lyo.Authentication.Web.Components`](../Lyo.Authentication.Web.Components/README.md). Plugs the shared login / debug / profile pages into the
BFF-cookie auth runtime in [`Lyo.Authentication.Client`](../Lyo.Authentication.Client/README.md).

## What's inside

| Service                     | Role                                                                                                                                                                                                                 |
|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ServerAuthSignInLauncher`  | Sign-in 302s through the consumer's `/auth/sign-in/{provider}` endpoint; sign-out submits a POST to `/auth/sign-out` via a dynamically constructed form (CSRF-safe).                                                 |
| `ServerAuthSessionAccessor` | Unseals the HttpOnly session cookie via `IDataProtectionProvider`, reads the active `LyoAuthSession` from `LyoAuthSessionStore`, and rotates it through `/auth/refresh` for the debug page's "Refresh token" button. |
| `ServerAuthUserClient`      | Typed `HttpClient` with `AddLyoAuthHandler()` so calls to `GET /auth/me` and `GET /auth/users/{id}` automatically carry the bearer and auto-refresh on 401.                                                          |

## Registration

```csharp
builder.Services.AddLyoAuthClient(builder.Configuration);
builder.Services.AddLyoAuthBlazorStateProvider();
builder.Services.AddLyoAuthWebComponents(builder.Configuration);
builder.Services.AddLyoAuthWebComponentsServer();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapLyoAuthSignIn();
app.MapLyoAuthHandoffCallback();
app.MapLyoAuthSignOut();
```

Make sure the consuming Blazor Server router picks up this package's pages — either via auto-scan or by adding the assembly to `AdditionalAssemblies`:

```razor
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(Lyo.Authentication.Web.Components.Pages.LoginPage).Assembly }">
```
