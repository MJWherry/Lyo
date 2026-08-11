using System.CommandLine;
using System.Text;
using Lyo.Cli.Services;
using Lyo.Common.Enums;
using Lyo.TextEncoding;

namespace Lyo.Cli.Commands;

internal static class EncCommands
{
    public static Command Create()
    {
        var enc = new Command("enc", "Binary and charset encoding (Lyo.TextEncoding). Not crypto — use 'crypt' for that.");
        foreach (var kind in new[] { "base64", "base64url", "hex" }) {
            var kindCmd = new Command(kind, $"Binary {kind} codec");
            kindCmd.Subcommands.Add(CreateBinary("encode", kind, encode: true));
            kindCmd.Subcommands.Add(CreateBinary("decode", kind, encode: false));
            enc.Subcommands.Add(kindCmd);
        }

        var charset = new Command("charset", "Character-set convert / detect");
        charset.Subcommands.Add(CreateCharsetConvert());
        charset.Subcommands.Add(CreateCharsetDetect());
        enc.Subcommands.Add(charset);
        return enc;
    }

    private static Command CreateBinary(string name, string kindName, bool encode)
    {
        var cmd = new Command(name, encode ? "Encode binary → text" : "Decode text → binary");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        var outputOpt = new Option<string?>("--output", "-o");
        var upperOpt = new Option<bool>("--upper") { Description = "Uppercase hex (default for encode)" };
        var lowerOpt = new Option<bool>("--lower") { Description = "Lowercase hex" };
        cmd.Arguments.Add(inputArg);
        cmd.Options.Add(outputOpt);
        if (encode && kindName == "hex") {
            cmd.Options.Add(upperOpt);
            cmd.Options.Add(lowerOpt);
        }

        cmd.SetAction(async (pr, ct) => {
            var kind = CliEncoding.ParseKind(kindName);
            var hexCase = pr.GetValue(lowerOpt) ? TextLetterCase.Lower : TextLetterCase.Upper;
            var (input, leaveIn, inPath) = CliIO.OpenInput(pr.GetValue(inputArg));
            try {
                if (encode) {
                    var (output, leaveOut, _) = CliIO.OpenOutput(pr.GetValue(outputOpt), inPath, p => p + "." + kindName);
                    try {
                        await using var writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: leaveOut);
                        await CliEncoding.EncodeAsync(kind, input, writer, hexCase, ct).ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                    finally {
                        if (!leaveOut)
                            await output.DisposeAsync().ConfigureAwait(false);
                    }
                }
                else {
                    var (output, leaveOut, _) = CliIO.OpenOutput(pr.GetValue(outputOpt), inPath, p => CliIO.StripExtension(p, "." + kindName));
                    try {
                        using var reader = new StreamReader(input, leaveOpen: leaveIn);
                        await CliEncoding.DecodeAsync(kind, reader, output, ct).ConfigureAwait(false);
                    }
                    finally {
                        if (!leaveOut)
                            await output.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally {
                if (!leaveIn)
                    await input.DisposeAsync().ConfigureAwait(false);
            }
        });

        return cmd;
    }

    private static Command CreateCharsetConvert()
    {
        var cmd = new Command("convert", "Convert bytes between charsets");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        var outputOpt = new Option<string?>("--output", "-o");
        var fromOpt = new Option<string>("--from") { Required = true };
        var toOpt = new Option<string>("--to") { Required = true };
        cmd.Arguments.Add(inputArg);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(fromOpt);
        cmd.Options.Add(toOpt);
        cmd.SetAction(async (pr, ct) => {
            var (input, leaveIn, inPath) = CliIO.OpenInput(pr.GetValue(inputArg));
            try {
                var (output, leaveOut, _) = CliIO.OpenOutput(pr.GetValue(outputOpt), inPath, p => p + ".converted");
                try {
                    await CliEncoding.ConvertCharsetAsync(input, output, pr.GetValue(fromOpt)!, pr.GetValue(toOpt)!, ct).ConfigureAwait(false);
                }
                finally {
                    if (!leaveOut)
                        await output.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally {
                if (!leaveIn)
                    await input.DisposeAsync().ConfigureAwait(false);
            }
        });
        return cmd;
    }

    private static Command CreateCharsetDetect()
    {
        var cmd = new Command("detect", "Detect charset of input");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        cmd.Arguments.Add(inputArg);
        cmd.SetAction(async (pr, ct) => {
            var (input, leaveIn, _) = CliIO.OpenInput(pr.GetValue(inputArg));
            try {
                var label = await CliEncoding.DetectCharsetAsync(input, ct).ConfigureAwait(false);
                await Console.Out.WriteLineAsync(label).ConfigureAwait(false);
            }
            finally {
                if (!leaveIn)
                    await input.DisposeAsync().ConfigureAwait(false);
            }
        });
        return cmd;
    }
}
