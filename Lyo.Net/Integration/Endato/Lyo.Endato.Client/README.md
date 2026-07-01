# Lyo.Endato.Client

Typed HTTP client for the [Endato](https://www.endato.com/) data-enrichment REST API.

**Archetype C (vendor client).** Canonical people rows live in [`Lyo.People.Postgres`](../../../Core/People/Lyo.People.Postgres/README.md) (Archetype A); optional vendor cache in [
`Lyo.Endato.Postgres`](../Lyo.Endato.Postgres/README.md) (Archetype D). Host maps Endato DTOs → `Person` + `person_source` (`EntitySourceRecord.From` with `EndatoPsPerson` /
`EndatoCePerson` on **`source_entity_*`**). See [package layout](../../../docs/package-layout.md).

Subclasses `Lyo.Api.Client.ApiClient` so JSON serialization, Accept-Encoding, and optional request compression behave the same as for any other Lyo HTTP client.

## Surface

[`EndatoClient`](EndatoClient.cs) wires the two galaxy-API endpoints behind manager properties:

| Property     | Manager                                                                     | HTTP call                                                                                                   |
|--------------|-----------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|
| `Persons`    | [`PersonManager.QueryPersonsAsync(query, ct)`](PersonManager.cs)            | `POST /PersonSearch` with header `galaxy-search-type: Person`. Returns `PersonQueryResponse`.               |
| `Enrichment` | [`EnrichmentManager.QueryEnrichmentAsync(query, ct)`](EnrichmentManager.cs) | `POST /Contact/Enrich` with header `galaxy-search-type: DevAPIContactEnrich`. Returns `EnrichmentResponse`. |

Both managers accept a built query or a [`PersonQueryBuilder`](Models/Person/Request/PersonQueryBuilder.cs) / [
`EnrichmentQueryBuilder`](Models/Enrichment/Request/EnrichmentQueryBuilder.cs).

Authentication is wired automatically: the client sets `galaxy-ap-name` / `galaxy-ap-password` headers from options on every request.

Models live under [`Models/Person`](Models/Person) (request + response, plus pagination) and [`Models/Enrichment`](Models/Enrichment).

### Request builders

Person Search — see [Person Search properties](https://enformiongo.readme.io/reference/person-search-properties):

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

Contact Enrichment — see [Contact Enrichment properties](https://enformiongo.readme.io/reference/contact-enrichment-properties). The API requires **at least two** of name, phone,
email, or address; `EnrichmentQueryBuilder.Build()` enforces that:

```csharp
var query = EnrichmentQueryBuilder.Create("John", "Smith")
    .WithPhone("5125550100")
    .WithAddress("123 Main St", "Austin, TX 78701")
    .Build();

var response = await client.Enrichment.QueryEnrichmentAsync(query, ct);
```

### Numeric fields

`EndatoClient` uses [`LyoJsonSerializerOptions`](../../../Core/Common/Lyo.Common/LyoJsonSerializerOptions.cs) (`AllowReadingFromString`), so latitude/longitude deserialize into
`decimal?` whether the API sends JSON numbers or quoted strings.

## Options ([`EndatoClientOptions`](EndatoClientOptions.cs))

Configuration section: `EndatoClient` (shadows the base `ApiClient` section). Inherits all
[`ApiClientOptions`](../../Api/Lyo.Api.Client/README.md#options-apiclientoptions) flags and adds:

| Property     | Description                                                      |
|--------------|------------------------------------------------------------------|
| `ApName`     | Endato AP name (sent as `galaxy-ap-name`). **Required.**         |
| `ApPassword` | Endato AP password (sent as `galaxy-ap-password`). **Required.** |

`BaseUrl` is required (validated in the constructor); point it at `https://api.endato.com` (or another Endato environment).

## DI registration ([`Extensions.cs`](Extensions.cs))

| Method                                                          | Description                                                                                                 |
|-----------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|
| `AddEndatoClientFromConfiguration(configuration, sectionName?)` | Binds `EndatoClientOptions` from configuration (default section `"EndatoClient"`) and registers the client. |
| `AddEndatoClient(Action<EndatoClientOptions> configure)`        | Builds options inline.                                                                                      |
| `AddEndatoClient(EndatoClientOptions options)`                  | Registers a pre-built options instance.                                                                     |

All overloads register `EndatoClient` as a singleton, pulling `ILoggerFactory` and any registered `HttpClient` from DI.

```csharp
services.AddEndatoClientFromConfiguration(builder.Configuration);

// or:
services.AddEndatoClient(o => {
    o.BaseUrl = "https://api.endato.com";
    o.ApName = "your-ap-name";
    o.ApPassword = "your-ap-password";
});
```

Example `appsettings.json`:

```json
{
  "EndatoClient": {
    "BaseUrl": "https://api.endato.com",
    "ApName": "your-ap-name",
    "ApPassword": "your-ap-password"
  }
}
```

## Related projects

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md) — base HTTP client and options.
- [`Lyo.Endato.Postgres`](../Lyo.Endato.Postgres/README.md) — PostgreSQL persistence for the same shapes.
- [`Lyo.Endato.Web.Components`](../Lyo.Endato.Web.Components/) — Blazor search/enrichment workbench UI (used by Gateway `/endato`).
