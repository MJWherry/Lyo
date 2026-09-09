using System.CommandLine;
using Lyo.Cli.Services;

namespace Lyo.Cli.Commands;

internal static class CryptCommands
{
    public static Command Create()
    {
        var crypt = new Command("crypt", "Encrypt and decrypt (Lyo.Encryption). Not the same as 'enc' (encoding).");
        crypt.Subcommands.Add(CreateTransform("encrypt", true));
        crypt.Subcommands.Add(CreateTransform("decrypt", false));
        return crypt;
    }

    private static Command CreateTransform(string name, bool encrypt)
    {
        var cmd = new Command(name, encrypt ? "Encrypt input" : "Decrypt input");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne, Description = "File path, '-', or omit when stdin is piped" };
        var outputOpt = new Option<string?>("--output", "-o") { Description = "Output path or '-'" };
        var keyOpt = new Option<string?>("--key") { Description = "Key (hex, base64, or passphrase)" };
        var keyFileOpt = new Option<string?>("--key-file") { Description = "File containing key material" };
        var algoOpt = new Option<string?>("--algorithm", "-a") { Description = "aesgcm (default), chacha20poly1305, xchacha20poly1305, aesccm, aessiv" };
        cmd.Arguments.Add(inputArg);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(keyOpt);
        cmd.Options.Add(keyFileOpt);
        cmd.Options.Add(algoOpt);
        cmd.SetAction(async (pr, ct) => {
            var algo = CliEncryption.ParseAlgorithm(pr.GetValue(algoOpt));
            var key = await KeyMaterial.ResolveForLengthAsync(pr.GetValue(keyOpt), pr.GetValue(keyFileOpt), CliEncryption.RequiredKeyBytes(algo), ct).ConfigureAwait(false);
            var inputPath = pr.GetValue(inputArg);
            var (input, leaveIn, inPath) = CliIO.OpenInput(inputPath);
            try {
                var ext = CliEncryption.FileExtension(algo);
                var (output, leaveOut, _) = CliIO.OpenOutput(pr.GetValue(outputOpt), inPath, p => encrypt ? CliIO.AppendExtension(p, ext) : CliIO.StripExtension(p, ext));
                try {
                    if (encrypt)
                        await CliEncryption.EncryptAsync(input, output, algo, key, ct).ConfigureAwait(false);
                    else
                        await CliEncryption.DecryptAsync(input, output, algo, key, ct).ConfigureAwait(false);
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
}