# Lyo.Cli

A `PackAsTool` dotnet tool (command name `lyo`) over Lyo libraries. **crypt** handles encryption (Lyo.Encryption); **enc** handles binary/charset encoding (Lyo.TextEncoding). The
remaining verbs are **compress**/ **decompress**, **hash**/ **checksum**, **id**, **query build|exec**, **csv**/ **xlsx**, **json**/ **yaml**/ **xml**/ **toml**, **nfc**, and
**rfid**. No dedicated CLI test project exists. System.CommandLine parses arguments; package tests cover library behavior.

## Features

- **crypt.** Single-key encrypt/decrypt (aesgcm default; file or pipe)
- **enc.** Base64 / base64url / hex encode and decode; charset convert/detect
- **compress** / **decompress.** Every Lyo.Compression algorithm (brotli default)
- **hash** / **checksum.** SHA-2/MD5, HMAC-SHA256, CRC/Adler, sparse fingerprint; `-o`, `-c/--copy`, `-q/--quiet`
- **id.** ULID, KSUID, NanoID, GUID variants, Snowflake; `-n/--count` for a batch; `-c/--copy` clipboard
- **query build|exec.** Lyo.Query.Models builders plus a Lyo.Api.Client POST
- **csv** / **xlsx.** Merge, split, convert, stats (xlsx→csv is native; csv→xlsx goes through DataTable glue)
- **json** / **yaml** / **xml** / **toml.** Convert between the four document formats. Omit the input path or pass `-` for stdin, and omit `--output` or pass `-` for stdout.
- **nfc.** Type 2 NDEF read/write over PC/SC (`readers`, `read`, `write text|uri`).
- **rfid.** `rfid tonie path` maps a SLIX-L UID to the box content path. `rfid tonie read` and `prepare` need an ISO 15693 reader, which is not registered yet.

## Examples

### Install on this machine

```bash
python3 scripts/cli/pack_install.py pack-install
lyo --help
```

### Tonie content path from a UID

```bash
lyo rfid tonie path E004035012345678
```

### Hash from stdin

```bash
cat file.bin | lyo hash sha256
```

### Mint a batch of IDs

```bash
lyo id ulid -n 10
lyo id guid v7 --count 5 -c -q
```

### Hash and copy the digest

```bash
lyo hash sha256 file.bin -c
```

### Workbook stats

```bash
lyo xlsx stats workbook.xlsx
lyo xlsx stats workbook.xlsx --sheet Sheet1
```

### Build a query payload

```bash
lyo query build concrete --where Status:eq:Active --amount 20 -o req.json
```

### Encrypt and decrypt through a pipe

```bash
lyo crypt encrypt secret.bin -o - --key 'passphrase' | lyo crypt decrypt - --key 'passphrase' > plain.bin
```

## Pack and install

The library pack (`scripts/nuget/build_nuget.py`) skips `Tools/`. Pack the tool with `dotnet pack Lyo.Net/Tools/Lyo.Cli/Lyo.Cli.csproj -c Release -o artifacts/cli` then
`dotnet tool install -g Lyo.Cli --add-source artifacts/cli`, or use `python3 scripts/cli/pack_install.py pack-install`.

## Copy to clipboard (`-c/--copy`)

Works on `id` and `hash`/`checksum` text results. Talks to native OS tools through Process stdin: Windows `clip`, macOS `pbcopy`, Linux `wl-copy` / `xclip` / `xsel`. No TextCopy
package. For bulk IDs: `-n/--count N` (one ID per line; all copied together when using `-c`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Compression.BZip2` (direct, lyo)
- `Lyo.Compression.Lz4` (direct, lyo)
- `Lyo.Compression.Lzma` (direct, lyo)
- `Lyo.Compression.Snappier` (direct, lyo)
- `Lyo.Compression.Xz` (direct, lyo)
- `Lyo.Compression.Zstd` (direct, lyo)
- `Lyo.Csv` (direct, lyo)
- `Lyo.Csv.Models` (direct, lyo)
- `Lyo.Desktop` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Encryption.AesCcm` (direct, lyo)
- `Lyo.Encryption.AesSiv` (direct, lyo)
- `Lyo.Encryption.XChaCha20Poly1305` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.Nfc` (direct, lyo)
- `Lyo.Nfc.Pcsc` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Rfid` (direct, lyo)
- `Lyo.Rfid.Tonie` (direct, lyo)
- `Lyo.TextEncoding` (direct, lyo)
- `Lyo.Toml` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `Lyo.Xlsx.Models` (direct, lyo)
- `Lyo.Xml` (direct, lyo)
- `Lyo.Yaml` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `System.CommandLine` `2.0.10` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `Dorssel.Security.Cryptography.AesExtra` `2.0.0` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `EasyCompressor.LZ4` `2.1.0` (transitive, third-party)
- `EasyCompressor.LZMA` `2.1.0` (transitive, third-party)
- `EasyCompressor.Snappier` `2.1.0` (transitive, third-party)
- `EasyCompressor.ZstdSharp` `2.1.0` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Joveler.Compression.XZ` `5.0.2` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `PCSC` `7.0.1` (transitive, third-party)
- `PCSC.Iso7816` `7.0.1` (transitive, third-party)
- `SharpZipLib` `1.4.2` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)
- `Tomlyn` `2.10.1` (transitive, third-party)
- `YamlDotNet` `18.1.0` (transitive, third-party)