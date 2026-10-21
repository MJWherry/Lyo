using System.CommandLine;
using Lyo.Common.Core.Conversion;
using Lyo.Rfid;
using Lyo.Rfid.Tonie;

namespace Lyo.Cli.Commands;

internal static class RfidCommands
{
    public static Command Create()
    {
        var rfid = new Command("rfid", "ISO 15693 RFID. Tonie SLIX-L content paths and custom-tag prep.");
        rfid.Subcommands.Add(CreateTonie());
        return rfid;
    }

    private static Command CreateTonie()
    {
        var tonie = new Command("tonie", "Tonie SLIX-L content path and custom-tag prep. read and prepare need an ISO 15693 reader.");
        tonie.Subcommands.Add(CreateToniePath());
        tonie.Subcommands.Add(CreateTonieRead());
        tonie.Subcommands.Add(CreateToniePrepare());
        return tonie;
    }

    private static Command CreateToniePath()
    {
        var cmd = new Command("path", "Print the SD-card directory and file name for a Tonie UID");
        var uidArg = new Argument<string>("uid") { Description = "16 hex characters, for example E004035012345678" };
        cmd.Arguments.Add(uidArg);
        Bind(
            cmd, (pr, ct) => {
                ct.ThrowIfCancellationRequested();
                var path = TonieContentPath.FromUid(ParseUid(pr.GetValue(uidArg)!));
                Console.WriteLine($"file {path.FileName}");
                Console.WriteLine($"directory {path.DirectoryName}");
                return Task.CompletedTask;
            });

        return cmd;
    }

    private static Command CreateTonieRead()
    {
        var cmd = new Command("read", "Read a Tonie SLIX-L tag. Requires a registered ISO 15693 reader.");
        var readerOpt = ReaderOption();
        var passwordOpt = PasswordOption();
        cmd.Options.Add(readerOpt);
        cmd.Options.Add(passwordOpt);
        Bind(
            cmd, async (pr, ct) => {
                var password = ParsePassword(pr.GetValue(passwordOpt));
                var transceiver = OpenIso15693(pr.GetValue(readerOpt));
                var uid = await TonieTagSession.InventoryAsync(transceiver, ct).ConfigureAwait(false);
                var memory = await TonieTagSession.ReadUserMemoryAsync(transceiver, uid, password, ct).ConfigureAwait(false);
                var path = TonieContentPath.FromUid(uid);
                Console.WriteLine($"uid {ByteConvert.ToHex(uid)}");
                Console.WriteLine($"file {path.FileName}");
                Console.WriteLine($"directory {path.DirectoryName}");
                Console.WriteLine(ByteConvert.ToHex(memory));
            });

        return cmd;
    }

    private static Command CreateToniePrepare()
    {
        var cmd = new Command("prepare", "Write a blank SLIX-L and enable privacy with 0F0F0F0F unless --password is set.");
        var readerOpt = ReaderOption();
        var passwordOpt = PasswordOption();
        cmd.Options.Add(readerOpt);
        cmd.Options.Add(passwordOpt);
        Bind(
            cmd, async (pr, ct) => {
                var password = ParsePassword(pr.GetValue(passwordOpt));
                var transceiver = OpenIso15693(pr.GetValue(readerOpt));
                var uid = await TonieTagSession.InventoryAsync(transceiver, ct).ConfigureAwait(false);
                await TonieTagSession.PrepareCustomTagAsync(transceiver, uid, null, password, ct).ConfigureAwait(false);
                var path = TonieContentPath.FromUid(uid);
                Console.WriteLine($"uid {ByteConvert.ToHex(uid)}");
                Console.WriteLine($"file {path.FileName}");
                Console.WriteLine($"directory {path.DirectoryName}");
            });

        return cmd;
    }

    private static Option<string?> ReaderOption() => new("--reader") { Description = "Reader name. Omit to use the first reader." };

    public static Option<string?> PasswordOption() => new("--password") { Description = "4-byte privacy password as 8 hex characters. Default is 0F0F0F0F." };

    public static IIso15693Transceiver OpenIso15693(string? readerName)
    {
        var which = string.IsNullOrWhiteSpace(readerName) ? "" : $" '{readerName}'";
        throw new RfidException($"No ISO 15693 reader{which} is registered. Tonie tags need a reader that speaks ISO 15693, such as a PN5180.");
    }

    private static byte[] ParseUid(string hex)
    {
        byte[] uid;
        try {
            uid = Convert.FromHexString(hex.Replace(" ", ""));
        }
        catch (FormatException) {
            throw new RfidException("UID must be 16 hex characters.");
        }

        if (uid.Length != 8)
            throw new RfidException("UID must be 16 hex characters.");

        return uid;
    }

    private static ReadOnlyMemory<byte>? ParsePassword(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        byte[] password;
        try {
            password = Convert.FromHexString(hex.Replace(" ", ""));
        }
        catch (FormatException) {
            throw new RfidException("Privacy password must be 8 hex characters.");
        }

        if (password.Length != 4)
            throw new RfidException("Privacy password must be 8 hex characters.");

        return password;
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
}