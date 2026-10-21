# Lyo.Nfc

Read and write NTAG21x user pages and NFC Forum NDEF (Text, URI, or a raw record). `INfcTag` is the page port: `MemoryNfcTag` for tests and dumps, `Lyo.Nfc.Pcsc` for a PC/SC
reader. ISO 15693 tags, including Tonie figures, live in `Lyo.Rfid`.

## Features

- **Pages.** `ReadPagesAsync` and `WritePageAsync` move 4-byte Type 2 pages. A page that is not 4 bytes throws.
- **NDEF.** `NdefTextRecord`, `NdefUriRecord`, and `NdefRecord.Raw`. `NdefType2` wraps the message in a `0x03` TLV and a `0xFE` terminator.
- **NTAG size.** `NtagMemory` knows NTAG213, NTAG215, and NTAG216. User memory starts at page 4. Config and lock pages are not written.

## Examples

### Write and read a Text record on an in-memory NTAG213

```csharp
var tag = new MemoryNfcTag(NtagMemory.Ntag213, "04AABBCCDDEEFF");
var message = NdefMessage.Of(NdefTextRecord.Create("en", "hello"));
await NfcNdef.WriteAsync(tag, NtagMemory.Ntag213, message, cancellationToken);
var text = (NdefTextRecord)(await NfcNdef.ReadAsync(tag, NtagMemory.Ntag213, cancellationToken)).Records[0];
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (direct, microsoft, netstandard2.0)