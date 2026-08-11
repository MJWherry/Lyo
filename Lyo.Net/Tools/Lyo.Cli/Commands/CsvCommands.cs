using System.CommandLine;
using Lyo.Cli.Services;

namespace Lyo.Cli.Commands;

internal static class CsvCommands
{
    public static Command Create()
    {
        var csv = new Command("csv", "CSV merge, split, convert, stats, validate");
        csv.Subcommands.Add(CreateMerge());
        csv.Subcommands.Add(CreateSplit());
        csv.Subcommands.Add(CreateToXlsx());
        csv.Subcommands.Add(CreateAppend());
        csv.Subcommands.Add(CreateStats());
        csv.Subcommands.Add(CreateValidate());
        return csv;
    }

    private static void AddDialect(Command cmd, out Option<char?> delim, out Option<char?> quote, out Option<string?> encoding, out Option<bool> noHeader)
    {
        delim = new Option<char?>("--delimiter");
        quote = new Option<char?>("--quote");
        encoding = new Option<string?>("--encoding");
        noHeader = new Option<bool>("--no-header");
        cmd.Options.Add(delim);
        cmd.Options.Add(quote);
        cmd.Options.Add(encoding);
        cmd.Options.Add(noHeader);
    }

    private static Command CreateMerge()
    {
        var cmd = new Command("merge", "Concatenate CSV files");
        var files = new Argument<string[]>("files") { Arity = ArgumentArity.OneOrMore };
        var output = new Option<string>("--output", "-o") { Required = true };
        var noHeaders = new Option<bool>("--no-headers") { Description = "Do not treat/write headers when combining" };
        AddDialect(cmd, out var delim, out var quote, out var encoding, out var noHeader);
        cmd.Arguments.Add(files);
        cmd.Options.Add(output);
        cmd.Options.Add(noHeaders);
        cmd.SetAction(async (pr, ct) => {
            var svc = CliCsv.Create(pr.GetValue(delim), pr.GetValue(quote), pr.GetValue(encoding), pr.GetValue(noHeader) ? false : null);
            await CliCsv.MergeAsync(pr.GetValue(files)!, pr.GetValue(output)!, includeHeaders: !pr.GetValue(noHeaders), svc, ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private static Command CreateSplit()
    {
        var cmd = new Command("split", "Split CSV by row count");
        var file = new Argument<string>("file");
        var rows = new Option<int>("--rows") { Required = true };
        var dir = new Option<string>("--dir", "-d") { Required = true };
        AddDialect(cmd, out var delim, out var quote, out var encoding, out var noHeader);
        cmd.Arguments.Add(file);
        cmd.Options.Add(rows);
        cmd.Options.Add(dir);
        cmd.SetAction(async (pr, ct) => {
            var svc = CliCsv.Create(pr.GetValue(delim), pr.GetValue(quote), pr.GetValue(encoding), pr.GetValue(noHeader) ? false : null);
            var paths = await CliCsv.SplitAsync(pr.GetValue(file)!, pr.GetValue(rows), pr.GetValue(dir)!, svc, ct).ConfigureAwait(false);
            foreach (var p in paths)
                await Console.Out.WriteLineAsync(p).ConfigureAwait(false);
        });
        return cmd;
    }

    private static Command CreateToXlsx()
    {
        var cmd = new Command("to-xlsx", "Convert CSV to XLSX");
        var input = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        var output = new Option<string>("--output", "-o") { Required = true };
        AddDialect(cmd, out var delim, out var quote, out var encoding, out var noHeader);
        cmd.Arguments.Add(input);
        cmd.Options.Add(output);
        cmd.SetAction(async (pr, ct) => {
            var csv = CliCsv.Create(pr.GetValue(delim), pr.GetValue(quote), pr.GetValue(encoding), pr.GetValue(noHeader) ? false : null);
            var xlsx = CliXlsx.Create();
            await CliTabularConvert.CsvToXlsxAsync(pr.GetValue(input), pr.GetValue(output)!, csv, xlsx, ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private static Command CreateAppend()
    {
        var cmd = new Command("append", "Append rows from a CSV file onto a target CSV");
        var target = new Argument<string>("target");
        var rows = new Argument<string>("rows");
        AddDialect(cmd, out var delim, out var quote, out var encoding, out var noHeader);
        cmd.Arguments.Add(target);
        cmd.Arguments.Add(rows);
        cmd.SetAction(async (pr, ct) => {
            var svc = CliCsv.Create(pr.GetValue(delim), pr.GetValue(quote), pr.GetValue(encoding), pr.GetValue(noHeader) ? false : null);
            await CliCsv.AppendAsync(pr.GetValue(target)!, pr.GetValue(rows)!, svc, ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private static Command CreateStats()
    {
        var cmd = new Command("stats", "CSV statistics as JSON");
        var input = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        var output = new Option<string?>("--output", "-o");
        AddDialect(cmd, out var delim, out var quote, out var encoding, out var noHeader);
        cmd.Arguments.Add(input);
        cmd.Options.Add(output);
        cmd.SetAction(async (pr, ct) => {
            var svc = CliCsv.Create(pr.GetValue(delim), pr.GetValue(quote), pr.GetValue(encoding), pr.GetValue(noHeader) ? false : null);
            var json = await CliCsv.StatsAsync(pr.GetValue(input), svc, ct).ConfigureAwait(false);
            await CliIO.WriteTextAsync(pr.GetValue(output), json, ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private static Command CreateValidate()
    {
        var cmd = new Command("validate", "Parse-validate CSV (empty schema)");
        var input = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        AddDialect(cmd, out var delim, out var quote, out var encoding, out var noHeader);
        cmd.Arguments.Add(input);
        cmd.SetAction(async (pr, ct) => {
            var svc = CliCsv.Create(pr.GetValue(delim), pr.GetValue(quote), pr.GetValue(encoding), pr.GetValue(noHeader) ? false : null);
            Environment.ExitCode = await CliCsv.ValidateAsync(pr.GetValue(input), svc, ct).ConfigureAwait(false);
        });
        return cmd;
    }
}
