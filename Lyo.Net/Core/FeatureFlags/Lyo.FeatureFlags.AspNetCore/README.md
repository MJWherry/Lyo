# Lyo.FeatureFlags.AspNetCore

UseFeatureFlags runs after routing and authentication, and before authorization, so a disabled flag is 404 instead of 401. It does not register IFeatureFlagService. The host still calls AddFeatureFlags or AddFeatureFlagsFromConfiguration. An open route does not resolve the service. A gated route with no service returns an empty 500. A flag that is off returns an empty 404 and does not run the handler.

## Features

- **[RequireFeature]** on an MVC controller or action. Several attributes must all be enabled.
- **Minimal APIs.** A method-group attribute is metadata. A lambda or a route group uses RequireFeature.
- **Caller mapping.** User id is lyo:user, then sub, then NameIdentifier, then Identity.Name. There is no tenant claim unless TenantClaimType is set. A space-delimited scope claim is split. Attributes stay empty unless ContextFactory sets them.
- **ContextFactory.** Replaces that mapping. A null return is an anonymous caller.

## Examples

### Gate a route

```csharp
builder.Services.AddFeatureFlags(o => o.Add("checkout").On());

app.UseRouting();
app.UseAuthentication();
app.UseFeatureFlags();
app.UseAuthorization();

app.MapGet("/checkout", Checkout).RequireFeature("checkout");
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FeatureFlags` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)