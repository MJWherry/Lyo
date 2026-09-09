# Lyo.TextEncoding

Span-first binary-to-text codecs and character-set helpers for buffers, streams, and files. Two injectable services — `IBinaryEncodingService` and `ICharsetEncodingService` — sit beside static `BinaryEncoding` / `CharsetEncoding`. The namespace is `Lyo.TextEncoding`, so `System.Text.Encoding` needs no alias. Code pages come from `System.Text.Encoding.CodePages`. Hex casing uses `TextLetterCase` from `Lyo.Common.Core`.

## Features

- **`IBinaryEncodingService` / `BinaryEncoding`.** Hex, Base64, Base64Url. Streaming decode, MIME line wrap, PEM helpers, `TryEncode` / `TryDecode`.
- **`ICharsetEncodingService` / `CharsetEncoding`.** Resolve CodePages, convert streams sync or async, `EmitBom`, `CharsetConvertingStream`.
- **`CharsetInfo`.** Catalog of well-known encodings plus `Custom(...)`. Pickers use `WellKnown`.
- **Detection.** BOM, a UTF-8 heuristic, and text declarations. For non-seekable streams: `ConsumedPrefix` + `CreateReplayStream`.
- **Fallbacks.** Optional `EncoderFallback` / `DecoderFallback`. BCL encodings are cloned. Singletons are never mutated.
- **DI.** `AddLyoBinaryEncoding`, `AddLyoCharsetEncoding`, `AddLyoTextEncoding`.

## Examples

### Wire into DI

```csharp
using System.Text;
using Lyo.TextEncoding.Registration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLyoTextEncoding();

using var sp = services.BuildServiceProvider();
var binary = sp.GetRequiredService<IBinaryEncodingService>();
var charset = sp.GetRequiredService<ICharsetEncodingService>();
Encoding utf8 = Encoding.UTF8; // no alias needed
```

### Binary encode, decode, and PEM

```csharp
var encoded = BinaryEncoding.Encode(BinaryEncodingKind.Base64, payload, lineLength: 76);
var pem = BinaryEncoding.EncodePem("CERTIFICATE", payload);
var bytes = BinaryEncoding.DecodePem(pem, out var label);

await BinaryEncoding.DecodeAsync(BinaryEncodingKind.Base64, textReader, binaryOut, ct);
```

### Charset convert, detect, and a converting stream

```csharp
CharsetEncoding.EnsureCodePagesRegistered();
var utf8Bytes = CharsetEncoding.Convert(winBytes, CharsetInfo.Windows1252, CharsetInfo.Utf8);

var detected = CharsetEncoding.DetectEncoding(nonSeekableStream);
using var replay = CharsetEncoding.CreateReplayStream(nonSeekableStream, detected);

using var converting = CharsetEncoding.CreateConvertingStream(sink, CharsetInfo.Windows1252, CharsetInfo.Utf8);
converting.Write(src);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `System.Memory` `4.6.3` (direct, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)