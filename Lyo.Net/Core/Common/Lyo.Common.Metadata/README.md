# Lyo.Common.Metadata

The record catalogs that turn the bare enums in [`Lyo.Common.Core`](../Lyo.Common.Core/README.md) into something descriptive live in `Lyo.Common.Metadata`: `MimeTypeInfo` is the IANA media-type catalog, `FileTypeInfo` maps extensions onto those rows, `LanguageCodeInfo` knows BCP-47 and ISO 639 codes, `LyoTypeInfo` is the type picker behind config, job, and filter editors.

Records and the extensions that read them ship together because they depend on each other — `LyoTypeInfo` uses `StringExtensions`, and `FileTypeInfo.FromMimeType` reads `MimeTypeInfo`. Splitting them would have produced a cycle rather than two packages.

This package references `Lyo.Common.Core`, so a reference here also gives you the primitives. Namespaces sit under the package name: `Lyo.Common.Metadata.Records`, `.Extensions`, and `.JsonConverters`.

## Features

- **File and media metadata.** `MimeTypeInfo` is the IANA catalog (`Value`, aliases, `FromValue` which strips `Content-Type` parameters, `HttpContentType`). `FileTypeInfo` is the file-type registry: name, extensions, category, and a `Mime` row. Several file types may share one MIME (`Unknown` / `Bin` / `Enc` all use `application/octet-stream`). Video rows cover MP4, MOV, MKV, AVI, MPEG, MPEG-TS, 3GP, and HLS. The two-key envelope suffix is `TwoKeyEnvelopeSuffix = "2k"`; `CommonStorageResolutionSuffixes` is what `Lyo.FileStorage` uses when persisted blobs have no explicit metadata. Byte-scale units live on `FileSizeUnitInfo`.
- **`LyoTypeInfo`.** Type-picker catalog: CLR types (`IsClr`) plus entries other packages register at startup, with `FromType` / `FromName` / `ByCategory`, widget hints via `LyoTypeEditorKind` (including Xml, Regex, and a Formatter hint), synthesized `T[]` and `List<T>` entries for registered elements, legacy aliases, `ToJson` / `TryValidateJson`, and an implicit conversion to `Type`.
- **Language and platform records.** `ProgrammingLanguageInfo`, `LanguageCodeInfo` (`FromISO639_1`, `FromBCP_47`, `FromISO639_3`), and `SocialPlatformInfo`.
- **Network and protocol records.** `HttpStatusCodeInfo`, `PortInfo` (well-known ports with `PortCategory`, `FromName` / `FromPort` / `ByCategory`, implicit `int`), and `HttpHeaderInfo` (RFC + Lyo contract + common `X-` names with `HttpHeaderCategory` / `HttpHeaderPresence`, `FromName` / `ByCategory`, implicit `string`). Vendor wire names stay in that vendor’s `.Models` package.
- **Geography and geometry.** `BoundingBox2D` and `GeographicInfo` (`ByCountry`, `ByState`, `FromAbbreviation`, `FromTimeZone`). In the split, `GeographicInfo` moved here from `Enums/`: it is a registry record that happened to live beside the enums it keys off.
- **Enum-to-record bridges** (`Extensions/`). `TypeInfoExtensions` (the `GetMimeTypeFromExtension` / `GetFileTypeFromExtension` / `FromMimeValue` family), `EnumMetadataExtensions` (`GetStringValue`, `GetDescription`, `IsSingleFlag<T>`), and `LanguageExtensions`.
- **Record converters** (`JsonConverters/`). `SocialPlatformInfoJsonConverter` and `LanguageCodeInfoJsonConverter`, including their `Nullable*` wrappers. They reference `System.Text.Json` directly rather than depending on [`Lyo.Common.Json`](../Lyo.Common.Json/README.md) — that indirection is what keeps the two packages from forming a cycle.

## Examples

### Look up file and MIME data

```csharp
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.Records;

FileTypeInfo type = "report.pdf".GetFileTypeFromExtension();
MimeTypeInfo mime = "clip.mkv".GetMimeTypeFromExtension();
string contentType = mime.HttpContentType;

string label = CountryCode.NZ.GetDescription();
```

### Pick a type

```csharp
using Lyo.Common.Metadata.Records;

// Drives config, job-parameter and query-filter editors.
LyoTypeInfo info = LyoTypeInfo.FromName("int");
Type runtime = LyoTypeInfo.DateTime; // implicit Type
var scalars = LyoTypeInfo.ByCategory(LyoTypeCategory.Scalar);

// Packages register their own non-CLR entries at startup.
LyoTypeInfo.Register(new("cron", LyoTypeCategory.Text, LyoTypeEditorKind.Text));
```

## Records versus enums

Enums live in `Lyo.Common.Core`; the records that describe them live here. MIME is a catalog (`MimeTypeInfo`), not an enum: a package that only needs `video/mp4` as a string still goes through Metadata. File types point at MIME rows (`FileTypeInfo.Mime`) so jpg/jpeg share `image/jpeg` and unknown/bin/enc share `application/octet-stream`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)