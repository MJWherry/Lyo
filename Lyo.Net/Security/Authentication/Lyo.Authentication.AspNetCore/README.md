# Lyo.Authentication.AspNetCore

ASP.NET Core integration for `Lyo.Authentication`. Three schemes coexist behind a single dispatcher:

- `LyoApiToken` — validates Format-B opaque tokens (`lyo_pat_live_...`)
- `LyoJwt` — validates Lyo-signed Ed25519 JWTs (`ey...`)
- `LyoBearer` (the default) — sniffs the `Authorization`/`X-Api-Key` header and forwards to one of the above

```csharp
services.AddLyoAuthentication(configuration);
services.AddInMemoryAuthenticationStores();
services.AddLyoApiTokenAuthentication();

app.UseAuthentication();
app.UseAuthorization();
app.MapLyoJwks();
```

Per-endpoint scope-based authorization uses the `scope:<name>` policy convention. The `ScopeAuthorizationPolicyProvider` creates these policies on demand, so endpoints can simply
do:

```csharp
endpoints.MapGet("/people", ...).RequireAuthorization("scope:people.read");
```

or, when working with `EndpointAuth` from `Lyo.Api`:

```csharp
GetAuth = EndpointAuth.RequireAuthorization("scope:people.read")
```
