# Portfolio (Lyo.Portfolio)

Blazor WebAssembly showcase for the in-process subset of Lyo: components that work without any backend (cache, locks, scheduler, encryption + compression, CSV/XLSX, text diff, rich
text editor, etc.), plus a couple of pages that talk to a remote Lyo.Api host via `Lyo.Api.Client` for the Query Builder.

## Hosting model

- `RootComponents.Add<App>("#app")` + `<HeadOutlet>("head::after")`.
- `Lyo.Portfolio.csproj` sets `BlazorDisableThrowNavigationException`, `WasmIgnorePdbLoadErrors`, and `InvariantGlobalization` (the dev server cannot serve ICU `.dat` files; invariant culture is fine for a portfolio app).
- The Router (`Components/Routes.razor`) uses `DefaultLayout = typeof(MainLayout)` and renders a MudBlazor 404 view via `LayoutView`.

## Routed pages

Every page uses a static `@page` directive (no constants). `Lyo.Portfolio.Constants.Page` lists the same routes for use from code, but the pages do not reference it.

| Route | Page | Component |
| -------------------- | -------------------- | ----------------------------------------------------------------------------- |
| `/` | `IdGeneratorPage` | `Lyo.Web.Components` `IdWorkbench` |
| `/csv-xlsx` | `CsvXlsxPage` | `CsvXlsxWorkbench` (in-process `Lyo.Csv` + `Lyo.Xlsx`) |
| `/encryption` | `EncryptionPage` | `FileTransformWorkbench` → `PortfolioFileTransformer` |
| `/text-diff` | `TextDiffPage` | `Lyo.Web.Components` `LyoTextDiffViewer` |
| `/rich-text-editor` | `RichTextEditorPage` | `Lyo.Web.Components` `LyoRichTextEditor` |
| `/cache` | `CachePage` | `CacheWorkbench` over `Lyo.Cache` local provider |
| `/locks` | `LocksPage` | `LockWorkbench` + `SemaphoreWorkbench` over `Lyo.Lock` local provider |
| `/metrics` | `MetricsPage` | `MetricsWorkbench` over `Lyo.Metrics` |
| `/schedule` | `SchedulePage` | `Lyo.Schedule.Web.Components` |
| `/diagnostics` | `DiagnosticsPage` | `Lyo.Diagnostic.Web.Components` |
| `/privacy-redaction` | `PrivacyPage` | `Lyo.Privacy.Web.Components` |
| `/query-builder` | `QueryBuilderPage` | `Lyo.Query.Web.Components`, calls a remote Portfolio API via `Lyo.Api.Client` |

The workbenches under `Components/Workbenches/` (`LockWorkbench`, `CsvXlsxWorkbench`, `CacheWorkbench`, `DataTablePreview`, `MetricsWorkbench`, `FileTransformWorkbench`,
`SemaphoreWorkbench`) are local copies tuned for the WASM environment.

## `PortfolioFileTransformer`

- **Forward direction** — compress first (if requested), then either single-key encrypt or two-key encrypt (DEK + KEK) over a stream.
- **Reverse direction** — decrypt first, then decompress.
- **Algorithms** — AES-GCM, ChaCha20-Poly1305, AES-CCM, AES-SIV, XChaCha20-Poly1305 (`Lyo.Encryption.*`). AES-GCM/CCM/SIV expose key-size selection; two-key mode picks `TwoKeyEncryptionService<TDek, TKek>` from a 5×5 algorithm matrix in `CreateTwoKeyEncryptionService`.
- **Key material** — `Secret` is hashed (SHA-256/512 depending on cipher) and truncated to the required key length in `DeriveSymmetricKey`; an in-memory `LocalKeyStore` placeholder is supplied to each service so no real key store is touched.
- **Compression** — Gzip, Brotli, XZ, Zstd, LZ4, bzip2, etc. via `Lyo.Compression.CompressionService(new() { DefaultAlgorithm = … })`. Forward returns `(compressed, ratio, spaceSaved%)`; reverse returns `(decompressed, expansionRatio)`.
- **Naming** — `BuildEncryptedName` appends the service's `.FileExtension` (e.g. `.ag`, `.chacha`, `.ccm`, `.xchacha`); `BuildDecryptedName`/`BuildDecompressedName` strip it back, or fall back to `.decrypted`/`.decompressed`.
- **Probe (`ProbeFile`)** — Header-only sniff that does not decrypt. It dispatches on the encryption filename suffix:
- Two-key envelopes → `EncryptionHeader.Read` then `AppendTwoKeyStreamLayoutFromHeader`, which exposes format version, DEK/KEK algorithm ids, key id/version, encrypted DEK length, header size, and the first sealed-chunk length prefix.
- AES-SIV envelopes → enforces 16-byte synthetic IV, reports the key id/version and payload length.
- Other single-key symmetric V1 envelopes (AES-GCM/CCM, ChaCha20-Poly1305, XChaCha20-Poly1305) → `TryAppendSymmetricAuthenticatedEnvelopeDetails` parses format byte + key id length + key version + nonce length + 16-byte tag, and adds a cipher hint based on nonce length (12-byte = AES-GCM/ChaCha-Poly/AES-CCM, 24-byte = XChaCha20-Poly1305).
- RSA / AES-GCM+RSA suffixes — reported as "different wire format, decrypt with matching service" because they aren't symmetric V1.
- No encryption extension — runs `DescribeCompressionMagicExtended` on the first bytes and reports gzip / zlib / Brotli / xz / zstd / lz4 / bzip2 / Snappy magic.

## Service registration (`Program.cs`)

- **Lyo infra** — `AddCsvService`, `AddXlsxService`, `AddCompressionService`, `AddLyoMetrics`, `AddScheduler`, `AddLocalCacheFromConfiguration(Configuration)`, `AddLocalLock(enableMetrics)`, `AddLocalKeyedSemaphore(enableMetrics)`.
- **Encryption workbench** — `AddScoped<PortfolioFileTransformer>()`.
- **API client** — `Configure<ApiClientOptions>(Configuration.GetSection(ApiClientOptions.SectionName))` + transient `ApiClientOptions` projection + `AddLyoApiClient()`. JSON serializer options come from `LyoJsonSerializerOptions.Create().AddLyoDateOnlyModelConverters()` with `WriteIndented = true`.
- **Web shell** — `AddScoped<IJsInterop, JsInterop>()`, a second `LyoJsonSerializerOptions` singleton (indented), `AddHttpClient`, `AddBlazoredLocalStorage`, `AddScoped<ClientStore>()`.
- **MudBlazor** — `AddMudServices(config => …)` with `PopoverOptions.ModalOverlay = true`, bottom-right snackbar (filled variant, 5 s, bug-report icon for errors), and a few transition tweaks.

## Configuration

`appsettings.json` only declares logging defaults, `AllowedHosts`, `CacheOptions` (`Enabled`, `EnableMetrics`), and empty `QRCodeOptions` / `BarcodeOptions` / `ImageSharpOptions` placeholders. The Query Builder page expects an `ApiClient:BaseUrl` to be added when you wire it to a Portfolio API.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` — (direct, lyo)
- `Lyo.Compression` — (direct, lyo)
- `Lyo.Compression.BZip2` — (direct, lyo)
- `Lyo.Compression.LZ4` — (direct, lyo)
- `Lyo.Compression.LZMA` — (direct, lyo)
- `Lyo.Compression.Snappier` — (direct, lyo)
- `Lyo.Compression.XZ` — (direct, lyo)
- `Lyo.Compression.Zstd` — (direct, lyo)
- `Lyo.Csv` — (direct, lyo)
- `Lyo.DataTable.Models` — (direct, lyo)
- `Lyo.Diagnostic.Web.Components` — (direct, lyo)
- `Lyo.Encryption` — (direct, lyo)
- `Lyo.Encryption.AesCcm` — (direct, lyo)
- `Lyo.Encryption.AesSiv` — (direct, lyo)
- `Lyo.Encryption.XChaCha20Poly1305` — (direct, lyo)
- `Lyo.Hashing` — (direct, lyo)
- `Lyo.Keystore` — (direct, lyo)
- `Lyo.Lock` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Privacy.Web.Components` — (direct, lyo)
- `Lyo.Query.Web.Components` — (direct, lyo)
- `Lyo.Schedule.Web.Components` — (direct, lyo)
- `Lyo.Scheduler` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `Lyo.Xlsx` — (direct, lyo)
- `Blazored.LocalStorage` `4.5.0` — (direct, third-party)
- `Microsoft.AspNetCore.Components.WebAssembly` `10.0.5` — (direct, microsoft)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Csv.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Privacy` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Schedule.Models` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Lyo.Xlsx.Models` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` — (transitive, third-party)
- `CsvHelper` `33.1.0` — (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` — (transitive, third-party)
- `Dorssel.Security.Cryptography.AesExtra` `2.0.0` — (transitive, third-party)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `EasyCompressor.LZ4` `2.1.0` — (transitive, third-party)
- `EasyCompressor.LZMA` `2.1.0` — (transitive, third-party)
- `EasyCompressor.Snappier` `2.1.0` — (transitive, third-party)
- `EasyCompressor.ZstdSharp` `2.1.0` — (transitive, third-party)
- `ExcelDataReader` `3.9.0` — (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` — (transitive, third-party)
- `Joveler.Compression.XZ` `5.0.2` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SharpZipLib` `1.4.2` — (transitive, third-party)
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.Collections.Immutable` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` — (transitive, microsoft)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)