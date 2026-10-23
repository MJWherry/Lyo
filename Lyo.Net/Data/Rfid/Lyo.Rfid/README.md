# Lyo.Rfid

Read and write ISO 15693 blocks through `IRfidTag`. `MemoryRfidTag` keeps the blocks in a buffer for tests and dumps. `Iso15693Tag.OpenAsync` inventories one tag on
`IIso15693Transceiver` and takes the block size from GET SYSTEM INFORMATION. `Iso15693Crc` is the ISO 15693 CRC-16 (poly 0x8408, init 0xFFFF, least-significant byte first). A
PN5180-class reader implements the transceiver. Type 2 NFC readers do not speak this protocol.

## Features

- **Blocks.** `ReadBlocksAsync` and `WriteBlockAsync` move whole blocks. A write that is not `BlockSize` bytes throws.
- **Memory size.** `OpenAsync` reads the 2-byte GET SYSTEM INFORMATION field. Block count is the first byte plus one (at most 256). Block size is the low 5 bits of the second byte
  plus one.
- **Transceiver.** One call carries the whole frame, CRC included. The caller builds the command; the reader only moves bytes.
- **CRC.** `Iso15693Crc.Append` adds the two CRC bytes. `Verify` throws `RfidException` when they do not match.

## Examples

### Write and read a block on an in-memory tag

```csharp
var tag = new MemoryRfidTag(blockSize: 4, blockCount: 8, "E004035012345678");
var written = new byte[] { 9, 8, 7, 6 };
await tag.WriteBlockAsync(1, written, cancellationToken);
var read = await tag.ReadBlocksAsync(0, 2, cancellationToken);
// read[4..8] is 09 08 07 06
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (direct, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)