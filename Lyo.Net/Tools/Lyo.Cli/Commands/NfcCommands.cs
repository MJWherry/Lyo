using System.CommandLine;
using Lyo.Common.Core.Conversion;
using Lyo.Nfc;
using Lyo.Nfc.Pcsc;

namespace Lyo.Cli.Commands;

internal static class NfcCommands
{
    public static Command Create()
    {
        var nfc = new Command("nfc", "Read and write Type 2 NFC tags (NDEF text and URI)");
        nfc.Subcommands.Add(CreateReaders());
        nfc.Subcommands.Add(CreateRead());
        nfc.Subcommands.Add(CreateWrite());
        return nfc;
    }

    private static void Bind(Command cmd, Func<ParseResult, CancellationToken, Task> body)
        => cmd.SetAction(async (pr, ct) => {
            try {
                await body(pr, ct).ConfigureAwait(false);
                return 0;
            }
            catch (Exception ex) {
                await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                return 1;
            }
        });

    private static Command CreateReaders()
    {
        var cmd = new Command("readers", "List PC/SC reader names");
        Bind(
            cmd, (pr, ct) => {
                ct.ThrowIfCancellationRequested();
                using var reader = new PcscNfcReader();
                foreach (var name in reader.ListReaders())
                    Console.WriteLine(name);

                return Task.CompletedTask;
            });

        return cmd;
    }

    private static Command CreateRead()
    {
        var cmd = new Command("read", "Read a Type 2 tag: UID and NDEF, or raw user pages when there is no NDEF TLV");
        var readerOpt = ReaderOption();
        cmd.Options.Add(readerOpt);
        Bind(cmd, async (pr, ct) => await ReadType2Async(pr.GetValue(readerOpt), ct).ConfigureAwait(false));
        return cmd;
    }

    private static Command CreateWrite()
    {
        var write = new Command("write", "Write an NDEF Text or URI record to a Type 2 tag");
        write.Subcommands.Add(CreateWriteText());
        write.Subcommands.Add(CreateWriteUri());
        return write;
    }

    private static Command CreateWriteText()
    {
        var cmd = new Command("text", "Write an NDEF Text record");
        var textArg = new Argument<string>("text");
        var langOpt = new Option<string>("--lang") { DefaultValueFactory = _ => "en", Description = "IANA language code" };
        var readerOpt = ReaderOption();
        cmd.Arguments.Add(textArg);
        cmd.Options.Add(langOpt);
        cmd.Options.Add(readerOpt);
        Bind(
            cmd, async (pr, ct) => {
                var message = NdefMessage.Of(NdefTextRecord.Create(pr.GetValue(langOpt) ?? "en", pr.GetValue(textArg)!));
                await WriteType2Async(pr.GetValue(readerOpt), message, ct).ConfigureAwait(false);
            });

        return cmd;
    }

    private static Command CreateWriteUri()
    {
        var cmd = new Command("uri", "Write an NDEF URI record");
        var uriArg = new Argument<string>("url");
        var readerOpt = ReaderOption();
        cmd.Arguments.Add(uriArg);
        cmd.Options.Add(readerOpt);
        Bind(
            cmd, async (pr, ct) => {
                var message = NdefMessage.Of(NdefUriRecord.Create(pr.GetValue(uriArg)!));
                await WriteType2Async(pr.GetValue(readerOpt), message, ct).ConfigureAwait(false);
            });

        return cmd;
    }

    private static Option<string?> ReaderOption() => new("--reader") { Description = "Reader name. Omit to use the first reader." };

    private static async Task ReadType2Async(string? readerName, CancellationToken cancellationToken)
    {
        using var reader = new PcscNfcReader();
        await using var tag = await reader.ConnectAsync(readerName, cancellationToken).ConfigureAwait(false);
        var layout = NtagMemory.FromCapabilityContainer(await tag.ReadPagesAsync(3, 1, cancellationToken).ConfigureAwait(false));
        Console.WriteLine($"uid {tag.UidHex}");
        var user = await NfcNdef.ReadUserAreaAsync(tag, layout, cancellationToken).ConfigureAwait(false);
        try {
            foreach (var record in NdefType2.Decode(user).Records)
                PrintRecord(record);
        }
        catch (NfcException) {
            PrintRaw(layout, user);
        }
    }

    private static async Task WriteType2Async(string? readerName, NdefMessage message, CancellationToken cancellationToken)
    {
        using var reader = new PcscNfcReader();
        await using var tag = await reader.ConnectAsync(readerName, cancellationToken).ConfigureAwait(false);
        var layout = NtagMemory.FromCapabilityContainer(await tag.ReadPagesAsync(3, 1, cancellationToken).ConfigureAwait(false));
        await NfcNdef.WriteAsync(tag, layout, message, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"uid {tag.UidHex}");
        Console.WriteLine($"wrote {layout.Name}");
    }

    private static void PrintRecord(NdefRecord record)
    {
        switch (record) {
            case NdefTextRecord text:
                Console.WriteLine($"text {text.Language} {text.Text}");
                break;
            case NdefUriRecord uri:
                Console.WriteLine($"uri {uri.Uri}");
                break;
            default:
                Console.WriteLine($"record {record.TypeNameFormat} {ByteConvert.ToHex(record.Type)} {ByteConvert.ToHex(record.Payload)}");
                break;
        }
    }

    private static void PrintRaw(NtagLayout layout, byte[] user)
    {
        for (var offset = 0; offset < user.Length; offset += 4) {
            var page = layout.UserStartPage + offset / 4;
            Console.WriteLine($"{page:X2}: {ByteConvert.ToHex(user.AsSpan(offset, 4))}");
        }
    }
}