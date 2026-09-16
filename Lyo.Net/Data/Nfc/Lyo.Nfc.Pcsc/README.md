# Lyo.Nfc.Pcsc

`AddPcscNfc` registers `PcscNfcReader` as `INfcReader`. Connect with a reader name, or null to use the first reader. On Linux install and start `pcscd` (`libpcsclite`). The APDUs are the ACR122U Type 2 set: GET UID `FF CA 00 00 00`, read four pages `FF B0 00 page 10`, write one page `FF D6 00 page 04` plus four data bytes. This driver does not speak ISO 15693, so it cannot see a Tonie figure.

## Features

- **Registration.** `services.AddPcscNfc()` adds a singleton `INfcReader`. There is no options type.
- **Linux.** The PC/SC service must be running (`pcscd`). Tests do not open a reader; they lock the APDU bytes.

## Examples

### Read the first reader's UID

```csharp
services.AddPcscNfc();
var readers = provider.GetRequiredService<INfcReader>();
await using var tag = await readers.ConnectAsync(readerName: null, cancellationToken);
Console.WriteLine(tag.UidHex);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Nfc` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `PCSC` `7.0.1` (direct, third-party)
- `PCSC.Iso7816` `7.0.1` (direct, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)