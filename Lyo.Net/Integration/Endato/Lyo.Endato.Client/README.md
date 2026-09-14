# Lyo.Endato.Client

Typed HTTP client for the [Endato](https://www.endato.com/) data-enrichment REST API.

**Archetype C (vendor client).** Canonical people rows live in [`Lyo.People.Postgres`](../../../Core/People/Lyo.People.Postgres/README.md) (Archetype A); optional vendor cache in [ `Lyo.Endato.Postgres`](../Lyo.Endato.Postgres/README.md) (Archetype D). The host maps Endato DTOs → `Person` + `person_source` (`EntitySourceRecord.From` with `EndatoPsPerson` / `EndatoCePerson` on `source_entity_*`). See [package layout](../../../docs/package-layout.md).

Subclasses `Lyo.Api.Client.ApiClient` so JSON serialization, Accept-Encoding, and optional request compression behave the same as any other Lyo HTTP client.

## Examples

### Wire into DI ([`Extensions.cs`](Extensions.cs))

```csharp
services.AddEndatoClientFromConfiguration(builder.Configuration);

// or:
services.AddEndatoClient(o => {
    o.BaseUrl = "https://api.endato.com";
    o.ApName = "your-ap-name";
    o.ApPassword = "your-ap-password";
});
```

### Wire into DI ([`Extensions.cs`](Extensions.cs)) (2)

```json
{
  "EndatoClient": {
    "BaseUrl": "https://api.endato.com",
    "ApName": "your-ap-name",
    "ApPassword": "your-ap-password"
  }
}
```

## Manager surface

[`EndatoClient`](EndatoClient.cs) hangs the two galaxy-API endpoints off manager properties:

| Property | Manager | HTTP call |
| ------------ | --------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `Persons` | [`PersonManager.QueryPersonsAsync(query, ct)`](PersonManager.cs) | `POST /PersonSearch` with header `galaxy-search-type: Person`. Returns `PersonQueryResponse`. |
| `Enrichment` | [`EnrichmentManager.QueryEnrichmentAsync(query, ct)`](EnrichmentManager.cs) | `POST /Contact/Enrich` with header `galaxy-search-type: DevAPIContactEnrich`. Returns `EnrichmentResponse`. |

Either manager takes a built query or a [`PersonQueryBuilder`](Models/Person/Request/PersonQueryBuilder.cs) / [
`EnrichmentQueryBuilder`](Models/Enrichment/Request/EnrichmentQueryBuilder.cs).

Auth is applied for you: every request gets `galaxy-ap-name` / `galaxy-ap-password` from options.

Types live under [`Models/Person`](Models/Person) (request + response, plus pagination) and [`Models/Enrichment`](Models/Enrichment).

## Building requests

Person Search. See [Person Search properties](https://enformiongo.readme.io/reference/person-search-properties):

```csharp
var query = PersonQueryBuilder.Create("Jane", "Doe", age: 42)
    .WithPhone("5125550100")
    .AddAddress("123 Main St", county: "Travis")
    .WithResultsPerPage(25)
    .Build();

var response = await client.Persons.QueryPersonsAsync(query, ct);
// or:
var response = await client.Persons.QueryPersonsAsync(
    PersonQueryBuilder.Create("Jane", "Doe", age: 42).WithPhone("5125550100"), ct);
```

Contact Enrichment. See [Contact Enrichment properties](https://enformiongo.readme.io/reference/contact-enrichment-properties). The API needs **at least two** of name, phone,
email, or address; `EnrichmentQueryBuilder.Build()` enforces that:

```csharp
var query = EnrichmentQueryBuilder.Create("John", "Smith")
    .WithPhone("5125550100")
    .WithAddress("123 Main St", "Austin, TX 78701")
    .Build();

var response = await client.Enrichment.QueryEnrichmentAsync(query, ct);
```

## Number deserialization

`EndatoClient` uses [`LyoJsonSerializerOptions`](../../../Core/Common/Lyo.Common.Json/LyoJsonSerializerOptions.cs) (`AllowReadingFromString`), so latitude/longitude land in `decimal?` whether the API sends JSON numbers or quoted strings.

## Settings ([`EndatoClientOptions`](EndatoClientOptions.cs))

Binds from section `EndatoClient` (overrides the base `ApiClient` section). Picks up every
[`ApiClientOptions`](../../Api/Lyo.Api.Client/README.md#options-apiclientoptions) flag and adds:

| Property | Description |
| ------------ | ---------------------------------------------------------------- |
| `ApName` | Endato AP name (sent as `galaxy-ap-name`). **Required.** |
| `ApPassword` | Endato AP password (sent as `galaxy-ap-password`). **Required.** |

`BaseUrl` is required (checked in the constructor); point it at `https://api.endato.com` (or another Endato environment).

## Wire into DI ([`Extensions.cs`](Extensions.cs))

| Method | Description |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `AddEndatoClientFromConfiguration(configuration, sectionName?)` | Reads `EndatoClientOptions` from configuration (default section `"EndatoClient"`) and registers the client. |
| `AddEndatoClient(Action<EndatoClientOptions> configure)` | Constructs options in place. |
| `AddEndatoClient(EndatoClientOptions options)` | Takes a ready-made options object. |

Every overload registers `EndatoClient` as a singleton and takes `ILoggerFactory` plus any registered `HttpClient` from DI.

Example `appsettings.json`:

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)