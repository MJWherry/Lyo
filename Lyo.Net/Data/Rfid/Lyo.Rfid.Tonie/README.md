# Lyo.Rfid.Tonie

ICODE SLIX-L helpers for a Toniebox tag you program yourself. `TonieContentPath.FromUid` turns an 8-byte UID that starts with `E0 04 03` into the SD-card directory and file name
(each half reversed). `SlixPrivacy.XorPassword` applies the NXP formula `Password XOR {Random, Random}` to the two-byte GET RANDOM NUMBER result. Custom tags use the datasheet
factory privacy password `0F0F0F0F` (`ToniePrivacy.NxpDefault`). `TonieTagSession` sends GET RANDOM, SET PASSWORD, block read/write, and ENABLE PRIVACY through
`Lyo.Rfid.IIso15693Transceiver`. A Type 2 NFC reader cannot carry those frames.

## Features

- **Content path.** File name is the first four UID bytes reversed. Directory name is the last four reversed. `E004035012345678` is file `500304E0` and directory `78563412`.
- **Privacy XOR.** A 4-byte password and a 2-byte random become the session password. `0F0F0F0F` with random `1234` is `1D3B1D3B`.
- **Prepare.** `PrepareCustomTagAsync` writes 32 bytes (zeros when omitted) and enables privacy so the box will accept the tag.

## Examples

### Content path from a UID

```csharp
var path = TonieContentPath.FromUid(Convert.FromHexString("E004035012345678"));
// path.FileName == "500304E0"
// path.DirectoryName == "78563412"
```

### Session password

```csharp
var xor = SlixPrivacy.XorPassword(ToniePrivacy.NxpDefault.Span, new byte[] { 0x12, 0x34 });
// 1D 3B 1D 3B
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Rfid` (direct, lyo)
- `System.Memory` `4.6.3` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)