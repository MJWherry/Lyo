---
name: add-lyo-package
description: Add a new package/project to the Lyo monorepo following the taxonomy-first layout. Use when creating a new Lyo.* project, library, vendor client, provider, Postgres persistence package, or test project, or when the user asks to add a package to Lyo.slnx.
---

# Add a Lyo Package

## Workflow

Copy this checklist and track progress:

```
- [ ] 1. Classify the package (archetype A-E)
- [ ] 2. Pick folder + assembly name
- [ ] 3. Create the csproj + project docs.json (render README)
- [ ] 4. Register in Lyo.slnx
- [ ] 5. Add dependencies via Directory.Packages.props
- [ ] 6. Create the sibling *.Tests project
- [ ] 7. Update package-layout.md inventory (if applicable)
- [ ] 8. Verify with dotnet build
```

### 1. Classify

Read the classification checklist in `Lyo.Net/docs/package-layout.md` and answer its questions in order. Result is an archetype:

| Archetype                   | Placement                                                      | Example                  |
|-----------------------------|----------------------------------------------------------------|--------------------------|
| A — Lyo canonical domain    | `Core/{Domain}/`                                               | `Lyo.People.Models`      |
| B — capability + provider   | `Communication\|Security/{Capability}/`                        | `Lyo.Translation.Google` |
| C — thin vendor client      | `Integration/{Vendor}/`                                        | `Lyo.Endato.Client`      |
| D — vendor product vertical | `Integration/{Vendor}/`                                        | `Lyo.Discord.Postgres`   |
| E — platform                | `Integration/Api\|Web\|Job`, `Data/*`, `Features/*`, `Tools/*` | `Lyo.Api.Export`         |

Enforce the dependency law: Core must never reference Integration, vendor SDKs, or vendor clients. Archetype C packages require the `.Client` suffix.

### 2. Name

Folder mirrors assembly: `{Area}/{Group}/Lyo.{X}/Lyo.{X}.csproj`. Standard suffixes: `.Models`, `.Postgres`, `.Client`, `.Web.Components`, `.Tests`, `.Benchmarks`.

### 3. Create csproj + README

`Directory.Build.props` supplies LangVersion/Nullable/ImplicitUsings/packaging metadata — do not repeat them. Template for a reusable library:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
        <Description>One-sentence package description.</Description>
        <PackageTags>space separated tags</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
    </PropertyGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="Lyo.{X}.Tests"/>
    </ItemGroup>

    <ItemGroup>
        <None Include="README.md" Pack="true" PackagePath="\"/>
    </ItemGroup>
</Project>
```

Variations: Blazor components use `Microsoft.NET.Sdk.Razor` + `net10.0` + `<FrameworkReference Include="Microsoft.AspNetCore.App"/>`; hosts/tests/benchmarks target `net10.0` only.

**Package docs (required for libraries):** create `{project}/docs.json` beside the README (copy [
`docs/catalog/templates/package.template.json`](docs/catalog/templates/package.template.json) as a starting point). Fill `id`, `name`, `area`, `tagline`, `description`, `features`,
`examples`, optional `benchmarks` / `sections`, and set `readmePath` to the project README. Schema: [
`docs/catalog/schema/package.schema.json`](docs/catalog/schema/package.schema.json). Then run:

```bash
python3 scripts/docs/project-docs.py render
```

That regenerates the package `README.md`, root README package list, portfolio content, and Blazor `wwwroot/catalog`. **`docs.json` is the only source of truth** — never hand-edit
generated READMEs, and never run `extract` (it overwrites JSON from README and is lossy).

### 4. Register in Lyo.slnx

Add `<Project Path="{Area}/{Group}/Lyo.{X}/Lyo.{X}.csproj" />` inside the matching `<Folder Name="/{Area}/{Group}/">` element of `Lyo.Net/Lyo.slnx`. Create the folder element if
missing.

### 5. Dependencies

Central Package Management: `PackageReference` entries carry no `Version`. New packages get a `<PackageVersion Include="..." Version="[x.y.z,)"/>` row in
`Lyo.Net/Directory.Packages.props` (keep alphabetical).

### 6. Tests

Create `Lyo.{X}.Tests` beside the source project (xUnit; copy an existing sibling test csproj). Register it in `Lyo.slnx` too. Method naming: `Method_Scenario_ExpectedResult`.
Postgres/Redis suites use Testcontainers.

### 7. Inventory

If the package is in Integration, Communication, or Security, add a row to the relevant inventory section of `Lyo.Net/docs/package-layout.md`.

### 8. Verify

```bash
dotnet build Lyo.Net/Lyo.slnx
dotnet test Lyo.Net/{Area}/{Group}/Lyo.{X}.Tests
```

## Reference files to copy patterns from

- DI/options pattern: `Lyo.Net/Integration/Reporting/Lyo.Reporting.Postgres/Extensions.cs`
- Multi-target csproj with conditional packages: `Lyo.Net/Core/Common/Lyo.Common/Lyo.Common.csproj`
- Blazor component csproj: `Lyo.Net/Integration/Job/Lyo.Job.Web.Components/Lyo.Job.Web.Components.csproj`
