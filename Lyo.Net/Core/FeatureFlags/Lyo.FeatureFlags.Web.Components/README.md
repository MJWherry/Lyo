# Lyo.FeatureFlags.Web.Components

FeatureFlagPanel lists every flag's name and mode from the in-process snapshot. AllowEdit defaults to false. A switch sets that flag on or off through IFeatureFlagUpdater. A percentage or rules flag loses that targeting when the switch changes. FeatureFlagView renders its child only when IFeatureFlagService.IsEnabled is true. A missing service or a disabled flag renders nothing. Register AddFeatureFlags or AddFeatureFlagsFromConfiguration on the host.

## Features

- **FeatureFlagPanel.** Read-only until AllowEdit is true. The switch is on only for mode On. A percentage or rules row says that switching replaces that flag with on or off.
- **FeatureFlagView.** Takes Name, ChildContent, and an optional FeatureFlagContext. Null context is an anonymous caller.

## Examples

### Panel and view

```razor
<FeatureFlagPanel AllowEdit="true"/>

<FeatureFlagView Name="checkout">
    <CheckoutForm/>
</FeatureFlagView>
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.FeatureFlags` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Primitives` `10.0.5` (transitive, microsoft)