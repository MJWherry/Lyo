# Lyo.Hashing

Digests (**SHA-256/384/512**), compatibility **MD5** (non-security fingerprints only), hexadecimal helpers (**`HexEncoding`**), optional **`HashingStream`**, sparse file
fingerprints, and **`IHashingService`** with a process-wide **`HashingService.Shared`** singleton (same idea as **`Random.Shared`**).

Hex letter casing uses **`TextLetterCase`** (**`Upper`** / **`Lower`**) from **`Lyo.Common`**.

## Choosing an API

- **`Hasher`** / **`HexEncoding`**: static, no allocation of a service — best for straightforward call sites.
- **`IHashingService`** / **`HashingService`**: injectable façade for hashing buffers, streams, files, **`ParseHex`** / **`EqualsHex`**, HMAC helpers, **`CreateHashingStream`**,
  etc.
- **`HashingService.Shared`**: default options when you do not use DI.

**`Lyo.Common`**: extension **`byte[].ToHexString()`** (**`ByteArrayHexExtensions`**) — lowercase hex, implemented in this assembly under the **`Lyo.Common`** namespace.

## Dependency injection

Add package **`Microsoft.Extensions.DependencyInjection.Abstractions`** (or transitively via **Lyo.Hashing**):

```csharp
using Lyo.Hashing;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLyoHashing(); // resolves HashingService.Shared

// Or custom defaults:
services.AddLyoHashing(o => o.DefaultHexLetterCase = TextLetterCase.Lower);

using var sp = services.BuildServiceProvider();
var hashing = sp.GetRequiredService<IHashingService>();
```

With **`AddLyoHashing(Action<HashingOptions>?)`**, a **`null`** callback registers **`HashingService.Shared`**. Otherwise a **`HashingOptions`** singleton and **`HashingService`**
are registered.

**`AddLyoHashing(HashingOptions options)`** registers the given options instance and **`HashingService`**.
