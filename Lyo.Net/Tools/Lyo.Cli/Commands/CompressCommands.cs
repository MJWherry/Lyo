using System.CommandLine;
using Lyo.Cli.Services;

namespace Lyo.Cli.Commands;

internal static class CompressCommands
{
    public static IEnumerable<Command> Create()
    {
        yield return CreateOne("compress", true);
        yield return CreateOne("decompress", false);
    }

    private static Command CreateOne(string name, bool compress)
    {
        var cmd = new Command(name, compress ? "Compress input" : "Decompress input");
        var inputArg = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne, Description = "File path, '-', or omit when stdin is piped" };
        var outputOpt = new Option<string?>("--output", "-o") { Description = "Output path or '-'" };
        var algoOpt = new Option<string?>("--algorithm", "-a") {
            Description = compress ? "Algorithm (default brotli). Also gzip, deflate, zlib, lz4, zstd, snappier, lzma, bzip2, xz" : "Algorithm; omit to auto-detect when possible"
        };

        cmd.Arguments.Add(inputArg);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(algoOpt);
        cmd.SetAction(async (pr, ct) => {
            var algoName = pr.GetValue(algoOpt);
            var algo = compress || !string.IsNullOrWhiteSpace(algoName) ? CliCompression.ParseAlgorithm(algoName) : null;
            var inputPath = pr.GetValue(inputArg);
            var (input, leaveIn, inPath) = CliIO.OpenInput(inputPath);
            try {
                var ext = algo?.Extension ?? ".br";
                var (output, leaveOut, _) = CliIO.OpenOutput(pr.GetValue(outputOpt), inPath, p => compress ? CliIO.AppendExtension(p, ext) : CliIO.StripExtension(p, ext));
                try {
                    if (compress)
                        await CliCompression.CompressAsync(input, output, algo!, ct).ConfigureAwait(false);
                    else
                        await CliCompression.DecompressAsync(input, output, algo, ct).ConfigureAwait(false);
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