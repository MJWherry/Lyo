# Lyo.Hashing

Digests (**SHA-256/384/512**), optional **MD5** (non-security fingerprints only), hexadecimal encoding (**`HexEncoding`**), incremental hashing (**`HashingStream`**), sparse file
fingerprints (**`SparseFileFingerprinter`**), and an injectable façade (**`IHashingService`** / **`HashingService`**). A process-wide default is exposed as **`HashingService.Shared`**
(analogous to **`Random.Shared`**).

The public contracts are **`IHashingService`**, **`Hasher`**, **`HexEncoding`**, **`HashingStream`**, and **`SparseFileFingerprinter`**; **`HashingService`** is the default **`IHashingService`**
implementation. With XML doc generation enabled in the repo, IntelliSense surfaces the same summaries as this README. Implementation types use `<inheritdoc />` where they mirror
the interfaces.

Hex letter casing for service helpers uses **`TextLetterCase`** (**`Upper`** / **`Lower`**) from **`Lyo.Common`**.

## Features

- **SHA-2** – One-shot buffer hashing on modern .NET; stream hashing via **`HashAlgorithm`**
- **MD5** – Legacy compatibility and fingerprints only (not for security)
- **`IHashingService`** – Buffers, streams, files, hex encode/parse, timing-safe equality, HMAC-SHA-256/512, fingerprinting, **`CreateHashingStream`**
- **`Hasher`** – Static digest helpers without allocating a service
- **`HexEncoding`** – Encode/decode hex with explicit casing
- **`byte[].ToHexString()`** – Extension in namespace **`Lyo.Hashing`** (**`ByteArrayHexExtensions`**) — lowercase hex for historical consistency
- **`HashingStream`** – Wrap any **`Stream`**; hash updates on read/write; **`GetHash()`** / **`GetHashHex`**
- **Sparse fingerprints** – **`SparseFileFingerprinter`** samples large files; MD5 of size + samples (and mtime for very large files)
- **DI** – **`AddLyoHashing`** registers **`HashingService.Shared`** or a configured **`HashingService`**

## Choosing an API

| Situation | Prefer |
|-----------|--------|
| One-off digest in a hot path, no DI | **`Hasher.ComputeSha256`** / **`HexEncoding.ToHexString`** |
| Tests, scripts, or **`Random.Shared`-style** access | **`HashingService.Shared`** |
| ASP.NET / hosted apps | Inject **`IHashingService`** via **`AddLyoHashing`** |
| Hash while copying or processing a stream | **`HashingStream`** or **`IHashingService.CreateHashingStream`** |
| “Did this huge file change?” without full read | **`FingerprintSampledFileAsync`** / **`SparseFileFingerprinter`** |

## Usage

### Dependency injection

```csharp
using Lyo.Common.Enums;
using Lyo.Hashing;
using Lyo.Hashing.Registration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Default process-wide singleton (HashingService.Shared)
services.AddLyoHashing();

// Or custom defaults:
services.AddLyoHashing(o =>
{
    o.DefaultHexLetterCase = TextLetterCase.Lower;
    o.FingerprintDefaults.SampleSize = 256;
});

// Or explicit options instance:
// services.AddLyoHashing(myOptions);

using var sp = services.BuildServiceProvider();
var hashing = sp.GetRequiredService<IHashingService>();
```

Use **`using Lyo.Hashing.Registration`** so extension methods **`AddLyoHashing`** resolve on **`IServiceCollection`**.

### Buffers and streams (service)

```csharp
var digest = hashing.Hash(ContentDigestAlgorithm.Sha256, payload);
var hex = hashing.ToHex(digest); // uses DefaultHexLetterCase from options

using var ms = new MemoryStream(payload);
var digest2 = hashing.Hash(ContentDigestAlgorithm.Sha256, ms);

var fileDigest = await hashing.HashFileAsync(ContentDigestAlgorithm.Sha512, "/path/to/file.bin", ct);
```

### Static **`Hasher`** (no service)

```csharp
var sha256 = Hasher.ComputeSha256(data);
var sha384 = Hasher.ComputeSha384(span);
var fromStream = Hasher.ComputeSha512(stream);

// Generic SHA-2 selector: digestBits 256, 384, or 512
var any = Hasher.ComputeSha2(256, data);
```

### Hex encode / parse / compare

```csharp
var upper = HexEncoding.ToHexString(digest, TextLetterCase.Upper);
var lower = HexEncoding.ToHexString(digest, TextLetterCase.Lower);
var roundTrip = HexEncoding.FromHex(upper);

// Timing-safe compare (length mismatch → false)
var ok = hashing.FixedTimeEquals(left, right);

// Parse expected hex then compare (invalid hex → false)
var matches = hashing.EqualsHex(digest, expectedHexChars);
```

### **`byte[]` extension** (lowercase hex)

```csharp
using Lyo.Hashing;

byte[] buf = [0xDE, 0xAD];
var s = buf.ToHexString(); // "dead" — always lowercase
```

### **`HashingStream`**

Wrap an inner stream; every byte read or written updates the hash. Call **`GetHash()`** when finished (or **`GetHashHex`** for a string). **`GetHashString()`** remains **uppercase** for
backward compatibility; prefer **`GetHashHex(TextLetterCase)`** for explicit casing.

```csharp
using System.Security.Cryptography;
using var inner = File.OpenRead(path);
using var hashingStream = new HashingStream(inner, SHA256.Create());
var buffer = new byte[8192];
int n;
while ((n = await hashingStream.ReadAsync(buffer, ct)) > 0) { /* process buffer */ }
var digest = hashingStream.GetHash();
```

When created via **`IHashingService.CreateHashingStream`**, the correct **`HashAlgorithm`** instance is chosen for **`ContentDigestAlgorithm`**.

### Sparse file fingerprint

For directory snapshots or “probably unchanged” checks without hashing entire files:

```csharp
byte[]? fp = await hashing.FingerprintSampledFileAsync(path, new FileInfo(path).Length, ct: ct);
// null if path does not exist

// Metadata-only (size + last write UTC); no content read
var metaHex = SparseFileFingerprinter.MetadataOnlyHex(fileSize, lastWriteTimeUtc);
```

Thresholds and sample sizes come from **`FileFingerprintOptions`** (service defaults in **`HashingOptions.FingerprintDefaults`**).

### HMAC

```csharp
var mac = hashing.HmacSha256(key, payload);
var mac512 = hashing.HmacSha512(key, payload);
```

Key lifecycle and storage are caller responsibilities.

## Configuration

### **`HashingOptions`**

| Property | Default | Description |
|----------|---------|-------------|
| **`DefaultHexLetterCase`** | **`Upper`** | Casing for **`IHashingService.ToHex`** when **`letterCase`** is omitted |
| **`FingerprintDefaults`** | **`FileFingerprintOptions.Default`** | Defaults passed to **`FingerprintSampledFileAsync`** when options argument is null |

### **`FileFingerprintOptions`**

| Property | Default | Description |
|----------|---------|-------------|
| **`LargeFileThreshold`** | 100 MiB | Above this, extra middle/end samples are read |
| **`VeryLargeThreshold`** | 1 GiB | Above this, uses mtime + smaller content sample |
| **`SampleSize`** | 128 bytes | Sample length for start/middle/end reads |
| **`VeryLargeSampleSize`** | 64 bytes | Content sample size in the very-large path |

### **`ContentDigestAlgorithm`**

| Value | Meaning |
|-------|---------|
| **`Sha256`** | SHA-256 |
| **`Sha384`** | SHA-384 |
| **`Sha512`** | SHA-512 |
| **`Md5`** | MD5 — **not for security** |

## Notes

- **MD5** and sparse fingerprints are for compatibility, change detection, or tooling — do not use them for passwords, signatures, or integrity where an attacker can influence
  inputs.
- **`HashFileAsync`** throws if the file is missing; **`FingerprintSampledFileAsync`** returns **`null`** when the path does not exist.
- On **netstandard2.0**, file hashing uses synchronous **`HashAlgorithm`** paths under the hood where async OS APIs are unavailable.


## Dependencies

*(Synchronized from `Lyo.Hashing.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package                                                 | Version   |
|---------------------------------------------------------|-----------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)`   |
| `System.Memory`                                         | `4.6.3`   | *(netstandard2.0 only)* |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)