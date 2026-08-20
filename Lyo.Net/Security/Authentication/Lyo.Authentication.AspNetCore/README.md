# Lyo.Authentication.AspNetCore

ASP.NET Core integration for `Lyo.Authentication`. Three schemes coexist behind a single dispatcher:

- `LyoApiToken.` Validates Format-B opaque tokens (`lyo_pat_live_...`) - `LyoJwt.` Validates Lyo-signed Ed25519 JWTs (`ey...`) - `LyoBearer` (the default). Sniffs the `Authorization`/`X-Api-Key` header and forwards to one of the above

```csharp services.AddLyoAuthentication(configuration); services.AddInMemoryAuthenticationStores(); services.AddLyoApiTokenAuthentication();

app.UseAuthentication(); app.UseAuthorization(); app.MapLyoJwks(); ```

Per-endpoint scope-based authorization uses the `scope:<name>` policy convention. `ScopeAuthorizationPolicyProvider` creates these policies on demand, so endpoints can do:

```csharp endpoints.MapGet("/people", ...).RequireAuthorization("scope:people.read"); ```

or, when working with `EndpointAuth` from `Lyo.Api`:

```csharp GetAuth = EndpointAuth.RequireAuthorization("scope:people.read") ```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Authentication` (direct, lyo)
- `Lyo.Common` (direct, lyo)
- `Lyo.Diagnostic` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)