using System.CommandLine;
using Lyo.Cli.Services;

namespace Lyo.Cli.Commands;

internal static class HashCommands
{
    public static IEnumerable<Command> Create()
    {
        yield return CreateHashRoot();
        yield return CreateChecksumRoot();
    }

    private static Command CreateHashRoot()
    {
        var hash = new Command("hash", "Content digests and HMAC (Lyo.Hashing)");
        foreach (var algo in new[] { "sha256", "sha384", "sha512", "md5" })
            hash.Subcommands.Add(CreateDigest(algo));
        hash.Subcommands.Add(CreateHmac());
        hash.Subcommands.Add(CreateFingerprint());
        return hash;
    }

    private static Command CreateChecksumRoot()
    {
        var checksum = new Command("checksum", "Non-cryptographic checksums (CRC/Adler)");
        foreach (var algo in new[] { "crc32", "crc32c", "crc64", "adler32" })
            checksum.Subcommands.Add(CreateChecksum(algo));
        return checksum;
    }

    private static Command CreateDigest(string algo)
    {
        var cmd = new Command(algo, $"Compute {algo}");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        AddEmitOptions(cmd, out var outputOpt, out var copyOpt, out var quietOpt, out var upperOpt);
        cmd.Arguments.Add(inputArg);
        cmd.SetAction(async (pr, ct) => {
            var algorithm = CliHashing.ParseDigest(algo);
            var upper = pr.GetValue(upperOpt);
            var inputPath = pr.GetValue(inputArg);
            string hex;
            if (!string.IsNullOrWhiteSpace(inputPath) && inputPath != "-")
                hex = await CliHashing.HashFileAsync(algorithm, inputPath, upper, ct).ConfigureAwait(false);
            else {
                var (input, leaveIn, _) = CliIO.OpenInput(inputPath);
                try {
                    hex = await CliHashing.HashAsync(algorithm, input, upper, ct).ConfigureAwait(false);
                }
                finally {
                    if (!leaveIn)
                        await input.DisposeAsync().ConfigureAwait(false);
                }
            }

            await CliIO.EmitTextAsync(hex, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private static Command CreateChecksum(string algo)
    {
        var cmd = new Command(algo, $"Compute {algo}");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        AddEmitOptions(cmd, out var outputOpt, out var copyOpt, out var quietOpt, out var upperOpt);
        cmd.Arguments.Add(inputArg);
        cmd.SetAction(async (pr, ct) => {
            var algorithm = CliHashing.ParseChecksum(algo);
            var (input, leaveIn, _) = CliIO.OpenInput(pr.GetValue(inputArg));
            try {
                var hex = await CliHashing.ChecksumAsync(algorithm, input, pr.GetValue(upperOpt), ct).ConfigureAwait(false);
                await CliIO.EmitTextAsync(hex, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
            }
            finally {
                if (!leaveIn)
                    await input.DisposeAsync().ConfigureAwait(false);
            }
        });
        return cmd;
    }

    private static Command CreateHmac()
    {
        var cmd = new Command("hmac-sha256", "HMAC-SHA-256");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        var keyOpt = new Option<string?>("--key");
        var keyFileOpt = new Option<string?>("--key-file");
        AddEmitOptions(cmd, out var outputOpt, out var copyOpt, out var quietOpt, out var upperOpt);
        cmd.Arguments.Add(inputArg);
        cmd.Options.Add(keyOpt);
        cmd.Options.Add(keyFileOpt);
        cmd.SetAction(async (pr, ct) => {
            var key = await KeyMaterial.ResolveAsync(pr.GetValue(keyOpt), pr.GetValue(keyFileOpt), ct).ConfigureAwait(false);
            var (input, leaveIn, _) = CliIO.OpenInput(pr.GetValue(inputArg));
            try {
                var hex = await CliHashing.HmacSha256Async(key, input, pr.GetValue(upperOpt), ct).ConfigureAwait(false);
                await CliIO.EmitTextAsync(hex, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
            }
            finally {
                if (!leaveIn)
                    await input.DisposeAsync().ConfigureAwait(false);
            }
        });
        return cmd;
    }

    private static Command CreateFingerprint()
    {
        var cmd = new Command("fingerprint", "Sparse file fingerprint (path preferred)");
        var inputArg = new Argument<string>("input") { Description = "File path" };
        AddEmitOptions(cmd, out var outputOpt, out var copyOpt, out var quietOpt, out var upperOpt);
        cmd.Arguments.Add(inputArg);
        cmd.SetAction(async (pr, ct) => {
            var hex = await CliHashing.FingerprintFileAsync(pr.GetValue(inputArg)!, pr.GetValue(upperOpt), ct).ConfigureAwait(false);
            await CliIO.EmitTextAsync(hex, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private static void AddEmitOptions(
        Command cmd,
        out Option<string?> outputOpt,
        out Option<bool> copyOpt,
        out Option<bool> quietOpt,
        out Option<bool> upperOpt)
    {
        outputOpt = new Option<string?>("--output", "-o");
        copyOpt = new Option<bool>("--copy", "-c") { Description = "Copy result to system clipboard" };
        quietOpt = new Option<bool>("--quiet", "-q") { Description = "Suppress stdout" };
        upperOpt = new Option<bool>("--upper") { Description = "Uppercase hex" };
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(copyOpt);
        cmd.Options.Add(quietOpt);
        cmd.Options.Add(upperOpt);
    }
}
