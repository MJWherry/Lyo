# Portfolio (Lyo.Portfolio)

Blazor WebAssembly showcase for the in-process subset of Lyo: components that work without any backend (cache, locks, scheduler, encryption + compression, CSV/XLSX, text diff, rich
text editor, etc.), plus a couple of pages that talk to a remote Lyo.Api host via `Lyo.Api.Client` for the Query Builder.

## Hosting model

`Program.cs` is a plain `WebAssemblyHostBuilder`:

- `RootComponents.Add<App>("#app")` + `<HeadOutlet>("head::after")`.
- `Lyo.Portfolio.csproj` sets `BlazorDisableThrowNavigationException`, `WasmIgnorePdbLoadErrors`, and `InvariantGlobalization` (the dev server cannot serve ICU `.dat` files;
  invariant culture is fine for a portfolio app).
- The Router (`Components/Routes.razor`) uses `DefaultLayout = typeof(MainLayout)` and renders a MudBlazor 404 view via `LayoutView`.

`MainLayout.razor` is a MudBlazor drawer + app bar. Dark mode is persisted under the `pref_theme_dark` local-storage key via `Blazored.LocalStorage`. Pages set their title by
injecting `[CascadingParameter] MainLayout?` and calling `Layout?.SetPageTitle("…")`.

## Routed pages

Every page uses a static `@page` directive (no constants). `Lyo.Portfolio.Constants.Page` lists the same routes for use from code, but the pages do not reference it.

| Route                | Page                 | Component                                                                     |
|----------------------|----------------------|-------------------------------------------------------------------------------|
| `/`                  | `IdGeneratorPage`    | `Lyo.Web.Components` `IdWorkbench`                                            |
| `/csv-xlsx`          | `CsvXlsxPage`        | `CsvXlsxWorkbench` (in-process `Lyo.Csv` + `Lyo.Xlsx`)                        |
| `/encryption`        | `EncryptionPage`     | `FileTransformWorkbench` → `PortfolioFileTransformer`                         |
| `/text-diff`         | `TextDiffPage`       | `Lyo.Web.Components` `LyoTextDiffViewer`                                      |
| `/rich-text-editor`  | `RichTextEditorPage` | `Lyo.Web.Components` `LyoRichTextEditor`                                      |
| `/cache`             | `CachePage`          | `CacheWorkbench` over `Lyo.Cache` local provider                              |
| `/locks`             | `LocksPage`          | `LockWorkbench` + `SemaphoreWorkbench` over `Lyo.Lock` local provider         |
| `/metrics`           | `MetricsPage`        | `MetricsWorkbench` over `Lyo.Metrics`                                         |
| `/schedule`          | `SchedulePage`       | `Lyo.Schedule.Web.Components`                                                 |
| `/diagnostics`       | `DiagnosticsPage`    | `Lyo.Diagnostic.Web.Components`                                               |
| `/privacy-redaction` | `PrivacyPage`        | `Lyo.Privacy.Web.Components`                                                  |
| `/query-builder`     | `QueryBuilderPage`   | `Lyo.Query.Web.Components`, calls a remote Portfolio API via `Lyo.Api.Client` |

The workbenches under `Components/Workbenches/` (`LockWorkbench`, `CsvXlsxWorkbench`, `CacheWorkbench`, `DataTablePreview`, `MetricsWorkbench`, `FileTransformWorkbench`,
`SemaphoreWorkbench`) are local copies tuned for the WASM environment.

## `PortfolioFileTransformer`

`Services/PortfolioFileTransformer.cs` is the engine behind `/encryption`. It accepts a `PortfolioUploadedFile` (read from an `IBrowserFile` via
`PortfolioFileTransformer.ReadBrowserFileAsync`, capped at `MaxUploadBytes = 100 MiB`) and a `PortfolioTransformOptions`, then runs compression and/or encryption in the requested
order (encrypt-after-compress on the forward path, decrypt-before-decompress on reverse).

- **Forward direction** — compress first (if requested), then either single-key encrypt or two-key encrypt (DEK + KEK) over a stream.
- **Reverse direction** — decrypt first, then decompress.
- **Algorithms** — AES-GCM, ChaCha20-Poly1305, AES-CCM, AES-SIV, XChaCha20-Poly1305 (`Lyo.Encryption.*`). AES-GCM/CCM/SIV expose key-size selection; two-key mode picks
  `TwoKeyEncryptionService<TDek, TKek>` from a 5×5 algorithm matrix in `CreateTwoKeyEncryptionService`.
- **Key material** — `Secret` is hashed (SHA-256/512 depending on cipher) and truncated to the required key length in `DeriveSymmetricKey`; an in-memory `LocalKeyStore` placeholder
  is supplied to each service so no real key store is touched.
- **Compression** — Gzip, Brotli, XZ, Zstd, LZ4, bzip2, etc. via `Lyo.Compression.CompressionService(new() { DefaultAlgorithm = … })`. Forward returns
  `(compressed, ratio, spaceSaved%)`; reverse returns `(decompressed, expansionRatio)`.
- **Naming** — `BuildEncryptedName` appends the service's `.FileExtension` (e.g. `.ag`, `.chacha`, `.ccm`, `.xchacha`); `BuildDecryptedName`/`BuildDecompressedName` strip it back,
  or fall back to `.decrypted`/`.decompressed`.
- **Probe (`ProbeFile`)** — Header-only sniff that does not decrypt. It dispatches on the encryption filename suffix:
    - Two-key envelopes → `EncryptionHeader.Read` then `AppendTwoKeyStreamLayoutFromHeader`, which exposes format version, DEK/KEK algorithm ids, key id/version, encrypted DEK
      length, header size, and the first sealed-chunk length prefix.
    - AES-SIV envelopes → enforces 16-byte synthetic IV, reports the key id/version and payload length.
    - Other single-key symmetric V1 envelopes (AES-GCM/CCM, ChaCha20-Poly1305, XChaCha20-Poly1305) → `TryAppendSymmetricAuthenticatedEnvelopeDetails` parses format byte + key id
      length + key version + nonce length + 16-byte tag, and adds a cipher hint based on nonce length (12-byte = AES-GCM/ChaCha-Poly/AES-CCM, 24-byte = XChaCha20-Poly1305).
    - RSA / AES-GCM+RSA suffixes — reported as "different wire format, decrypt with matching service" because they aren't symmetric V1.
    - No encryption extension — runs `DescribeCompressionMagicExtended` on the first bytes and reports gzip / zlib / Brotli / xz / zstd / lz4 / bzip2 / Snappy magic.

`PortfolioFileTransformer.FormatErrors(IReadOnlyList<Error>?)` and `ParseLines(string?)` are exposed as utilities for the Razor workbench (multi-line input parsing, error
summarisation).

## Service registration (`Program.cs`)

The host registers everything the WASM build can actually run:

- **Lyo infra** — `AddCsvService`, `AddXlsxService`, `AddCompressionService`, `AddLyoMetrics`, `AddScheduler`, `AddLocalCacheFromConfiguration(Configuration)`,
  `AddLocalLock(enableMetrics)`, `AddLocalKeyedSemaphore(enableMetrics)`.
- **Encryption workbench** — `AddScoped<PortfolioFileTransformer>()`.
- **API client** — `Configure<ApiClientOptions>(Configuration.GetSection(ApiClientOptions.SectionName))` + transient `ApiClientOptions` projection + `AddLyoApiClient()`. JSON
  serializer options come from `LyoJsonSerializerOptions.Create().AddLyoDateOnlyModelConverters()` with `WriteIndented = true`.
- **Web shell** — `AddScoped<IJsInterop, JsInterop>()`, a second `LyoJsonSerializerOptions` singleton (indented), `AddHttpClient`, `AddBlazoredLocalStorage`,
  `AddScoped<ClientStore>()`.
- **MudBlazor** — `AddMudServices(config => …)` with `PopoverOptions.ModalOverlay = true`, bottom-right snackbar (filled variant, 5 s, bug-report icon for errors), and a few
  transition tweaks.

There is no auth, no Postgres, no S3, no message queue — Portfolio runs entirely in the browser apart from the optional Query Builder backend.

## Configuration

`appsettings.json` only declares logging defaults, `AllowedHosts`, `CacheOptions` (`Enabled`, `EnableMetrics`), and empty `QRCodeOptions` / `BarcodeOptions` / `ImageSharpOptions`
placeholders. The Query Builder page expects an `ApiClient:BaseUrl` to be added when you wire it to a Portfolio API.

## Related projects

- [`Lyo.Cache`](../../Core/Cache/Lyo.Cache/README.md)
- [`Lyo.Compression`](../../Data/Compression/Lyo.Compression/README.md)
- [`Lyo.Csv`](../../Data/Csv/Lyo.Csv/README.md)
- [`Lyo.DataTable.Models`](../../Data/DataTable/Lyo.DataTable.Models/README.md)
- [`Lyo.Diagnostic.Web.Components`](../../Core/Diagnostic/Lyo.Diagnostic.Web.Components/README.md)
- [`Lyo.Encryption`](../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.Hashing`](../../Security/Hashing/Lyo.Hashing/README.md)
- [`Lyo.Keystore`](../../Security/Encryption/Lyo.Keystore/README.md)
- [`Lyo.Lock`](../../Core/Lock/Lyo.Lock/README.md)
- [`Lyo.Metrics`](../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Privacy.Web.Components`](../../Core/Privacy/Lyo.Privacy.Web.Components/README.md)
- [`Lyo.Query.Web.Components`](../../Data/Query/Lyo.Query.Web.Components/README.md)
- [`Lyo.Schedule.Web.Components`](../../Core/Schedule/Lyo.Schedule.Web.Components/README.md)
- [`Lyo.Scheduler`](../../Core/Scheduler/Lyo.Scheduler/README.md)
- [`Lyo.Web.Components`](../../Integration/Web/Lyo.Web.Components/README.md)
- [`Lyo.Xlsx`](../../Data/Xlsx/Lyo.Xlsx/README.md)
