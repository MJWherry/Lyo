# Lyo.FeatureFlags

IFeatureFlagService.IsEnabled reads a snapshot taken at registration. An unknown name is off. AddFeatureFlags, an options instance, or AddFeatureFlagsFromConfiguration all register that service. The first registration wins. Percentage hashes the flag name with a stickiness key into a stable bucket. Rules match user, tenant, claim, and attribute. Apps pass a FeatureFlagContext. HTTP hosts use Lyo.FeatureFlags.AspNetCore.

## Features

- **IFeatureFlagService.** IsEnabled(name, context). Whitespace names throw. Names compare ordinal-ignore-case.
- **Modes.** Off, On, Percentage, and Rules. On ignores the caller. Percentage 0 is off and 100 is on. A missing name is off.
- **Stickiness.** User, Tenant, or a named attribute. StickinessKey on the context overrides that choice for a partial percentage.
- **Rules.** Rules are OR. Predicates inside one rule are AND. A claim or attribute matches when any caller pair equals.
- **Fluent Add.** On, Percentage, StickToTenant, StickToAttribute, WhenClaim, and When for an AND rule. The last mode call wins.
- **Configuration.** Section FeatureFlags. Each Flags child is bound on its own. A bad Mode throws ConfigurationException. A missing section leaves every flag off.

## Examples

### Registration

```csharp
services.AddFeatureFlags(o => o
    .Add("checkout").On()
    .Add("billing").Percentage(10)
    .Add("billing-tenant").Percentage(10).StickToTenant()
    .Add("admin").WhenClaim("role", "admin")
    .Add("acme-admin").When(r => r.Tenant("acme").Claim("role", "admin")));

services.AddFeatureFlagsFromConfiguration(configuration);

var caller = FeatureFlagContext.Create().WithUser(userId).WithTenant(tenantId).Build();
if (flags.IsEnabled("billing", caller))
    UseNewBilling();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)