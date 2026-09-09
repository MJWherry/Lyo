# Lyo.Configuration

One helper, `LyoOptions.Bind`, used by every `Add{Feature}FromConfiguration` overload in the suite: start from the options type's declared defaults, overlay whatever the named section provides, then apply the caller's `Action<TOptions>` so values supplied in code win.

It is a package rather than a method on `Lyo.Common.Core` because it is the only thing in the suite that needs `Microsoft.Extensions.Configuration.Binder`. Keeping it separate leaves that dependency off the ~90 projects that reference `Lyo.Common.Core` for records, enums, and extensions and never touch configuration.

## Features

- **Uniform binding.** Every package uses the same three-step path: `LyoOptions.Bind<TOptions>(configuration, sectionName, configure)`.
- **Missing sections are not errors.** Defaults on the options type stay in place when the section is absent or empty.
- **Code wins over configuration.** Binding finishes first; then the optional `configure` delegate runs.

## Examples

### House `FromConfiguration` pattern

```csharp
using Lyo.Configuration;

public IServiceCollection AddReportingDbContextFactoryFromConfiguration(
    IConfiguration configuration,
    string configSectionName = PostgresReportingOptions.SectionName)
{
    var options = LyoOptions.Bind<PostgresReportingOptions>(configuration, configSectionName);
    return services.AddReportingDbContextFactory(options);
}
```

## Why check `Exists()` first

Binding a section that does not exist still succeeds silently, so `section.Bind(options)` alone would work. Checking `Exists()` first makes the intent explicit — a missing section means "defaults are fine", not "bind nothing and hope" — and it keeps the behaviour identical across every package rather than depending on each author's reading of the binder.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)