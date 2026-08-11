# Lyo.Cli

dotnet tool (`PackAsTool`, command name `lyo`) that wraps Lyo libraries. **crypt** is encryption (Lyo.Encryption); **enc** is binary/charset encoding (Lyo.TextEncoding). **compress**/**decompress**, **hash**/**checksum**, **id**, **query build|exec**, and **csv**/**xlsx** cover the rest. No dedicated CLI test project — parsing uses System.CommandLine; library behavior is covered by package tests.

## Features

- **crypt** — single-key encrypt/decrypt (aesgcm default; file or pipe)
- **enc** — base64 / base64url / hex encode/decode; charset convert/detect
- **compress** / **decompress** — all Lyo.Compression algorithms (brotli default)
- **hash** / **checksum** — SHA-2/MD5, HMAC-SHA256, CRC/Adler, sparse fingerprint; `-o`, `-c/--copy`, `-q/--quiet`
- **id** — ULID, KSUID, NanoID, GUID variants, Snowflake; `-n/--count` for bulk; `-c/--copy` clipboard
- **query build|exec** — Lyo.Query.Models builders + Lyo.Api.Client POST
- **csv** / **xlsx** — merge, split, convert, stats (xlsx→csv native; csv→xlsx via DataTable glue)

## Examples

### Install locally

```bash
python3 scripts/cli/pack_install.py pack-install
lyo --help
```

### Hash stdin

```bash
cat file.bin | lyo hash sha256
```

### Generate many IDs

```bash
lyo id ulid -n 10
lyo id guid v7 --count 5 -c -q
```

### Hash and copy digest

```bash
lyo hash sha256 file.bin -c
```

### XLSX stats

```bash
lyo xlsx stats workbook.xlsx
lyo xlsx stats workbook.xlsx --sheet Sheet1
```

### Build a query body

```bash
lyo query build concrete --where Status:eq:Active --amount 20 -o req.json
```

### Encrypt / decrypt pipe

```bash
lyo crypt encrypt secret.bin -o - --key 'passphrase' | lyo crypt decrypt - --key 'passphrase' > plain.bin
```

## Pack / install

Library pack (`scripts/nuget/build_nuget.py`) skips `Tools/`. Pack this tool with `dotnet pack Lyo.Net/Tools/Lyo.Cli/Lyo.Cli.csproj -c Release -o artifacts/cli` then `dotnet tool install -g Lyo.Cli --add-source artifacts/cli`, or use `python3 scripts/cli/pack_install.py pack-install`.

## Clipboard (`-c/--copy`)

Available on `id` and `hash`/`checksum` text results. Uses native OS tools via Process stdin: Windows `clip`, macOS `pbcopy`, Linux `wl-copy` / `xclip` / `xsel`. No TextCopy package. Bulk IDs: `-n/--count N` (one ID per line; all copied together when using `-c`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Api.Models` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.Compression` — (direct, lyo)
- `Lyo.Compression.BZip2` — (direct, lyo)
- `Lyo.Compression.LZ4` — (direct, lyo)
- `Lyo.Compression.LZMA` — (direct, lyo)
- `Lyo.Compression.Snappier` — (direct, lyo)
- `Lyo.Compression.XZ` — (direct, lyo)
- `Lyo.Compression.Zstd` — (direct, lyo)
- `Lyo.Csv` — (direct, lyo)
- `Lyo.Csv.Models` — (direct, lyo)
- `Lyo.Encryption` — (direct, lyo)
- `Lyo.Encryption.AesCcm` — (direct, lyo)
- `Lyo.Encryption.AesSiv` — (direct, lyo)
- `Lyo.Encryption.XChaCha20Poly1305` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Hashing` — (direct, lyo)
- `Lyo.Keystore` — (direct, lyo)
- `Lyo.Query.Models` — (direct, lyo)
- `Lyo.TextEncoding` — (direct, lyo)
- `Lyo.Xlsx` — (direct, lyo)
- `Lyo.Xlsx.Models` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (direct, microsoft)
- `System.CommandLine` `2.0.10` — (direct, microsoft)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` — (transitive, third-party)
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
- `Microsoft.Extensions.Configuration` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SharpZipLib` `1.4.2` — (transitive, third-party)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` — (transitive, microsoft)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)