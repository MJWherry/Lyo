using System.CommandLine;
using Lyo.Cli.Services;

namespace Lyo.Cli.Commands;

internal static class XlsxCommands
{
    public static Command Create()
    {
        var xlsx = new Command("xlsx", "XLSX merge, split, convert, sheets, stats");
        xlsx.Subcommands.Add(CreateSheets());
        xlsx.Subcommands.Add(CreateStats());
        xlsx.Subcommands.Add(CreateMerge());
        xlsx.Subcommands.Add(CreateSplit());
        xlsx.Subcommands.Add(CreateToCsv());
        xlsx.Subcommands.Add(CreateFromCsv());
        return xlsx;
    }

    private static Command CreateStats()
    {
        var cmd = new Command("stats", "XLSX workbook statistics as JSON");
        var input = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        var output = new Option<string?>("--output", "-o");
        var sheet = new Option<string?>("--sheet") { Description = "Limit to one sheet (name or 0-based index)" };
        var noHeader = new Option<bool>("--no-header") { Description = "Treat first row as data (not headers)" };
        cmd.Arguments.Add(input);
        cmd.Options.Add(output);
        cmd.Options.Add(sheet);
        cmd.Options.Add(noHeader);
        cmd.SetAction(async (pr, ct) => {
            var hasHeader = pr.GetValue(noHeader) ? false : (bool?)null;
            var json = CliXlsx.Stats(pr.GetValue(input), pr.GetValue(sheet), hasHeader, CliXlsx.Create());
            await CliIO.WriteTextAsync(pr.GetValue(output), json, ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private static Command CreateSheets()
    {
        var cmd = new Command("sheets", "List sheet names");
        var file = new Argument<string>("file");
        cmd.Arguments.Add(file);
        cmd.SetAction(pr => {
            foreach (var name in CliXlsx.ListSheets(pr.GetValue(file)!, CliXlsx.Create()))
                Console.WriteLine(name);
        });
        return cmd;
    }

    private static Command CreateMerge()
    {
        var cmd = new Command("merge", "Merge XLSX files");
        var files = new Argument<string[]>("files") { Arity = ArgumentArity.OneOrMore };
        var output = new Option<string>("--output", "-o") { Required = true };
        var mode = new Option<string?>("--mode") { Description = "preserve (default) or concat" };
        cmd.Arguments.Add(files);
        cmd.Options.Add(output);
        cmd.Options.Add(mode);
        cmd.SetAction(pr => {
            CliXlsx.Merge(pr.GetValue(files)!, pr.GetValue(output)!, CliXlsx.ParseMode(pr.GetValue(mode)), CliXlsx.Create());
        });
        return cmd;
    }

    private static Command CreateSplit()
    {
        var cmd = new Command("split", "Split XLSX by sheet or rows");
        var file = new Argument<string>("file");
        var by = new Option<string>("--by") { Required = true, Description = "sheet | rows" };
        var rows = new Option<int?>("--rows");
        var dir = new Option<string>("--dir", "-d") { Required = true };
        var sheet = new Option<string?>("--sheet");
        cmd.Arguments.Add(file);
        cmd.Options.Add(by);
        cmd.Options.Add(rows);
        cmd.Options.Add(dir);
        cmd.Options.Add(sheet);
        cmd.SetAction(pr => {
            var paths = CliXlsx.Split(pr.GetValue(file)!, pr.GetValue(by)!, pr.GetValue(rows), pr.GetValue(dir)!, pr.GetValue(sheet), CliXlsx.Create());
            foreach (var p in paths)
                Console.WriteLine(p);
        });
        return cmd;
    }

    private static Command CreateToCsv()
    {
        var cmd = new Command("to-csv", "Convert XLSX to CSV (first sheet, or --sheet)");
        var input = new Argument<string>("input");
        var output = new Option<string>("--output", "-o") { Required = true };
        var sheet = new Option<string?>("--sheet");
        cmd.Arguments.Add(input);
        cmd.Options.Add(output);
        cmd.Options.Add(sheet);
        cmd.SetAction(pr => CliXlsx.ToCsv(pr.GetValue(input)!, pr.GetValue(output)!, pr.GetValue(sheet), CliXlsx.Create()));
        return cmd;
    }

    private static Command CreateFromCsv()
    {
        var cmd = new Command("from-csv", "Convert CSV to XLSX (alias of csv to-xlsx)");
        var input = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne };
        var output = new Option<string>("--output", "-o") { Required = true };
        cmd.Arguments.Add(input);
        cmd.Options.Add(output);
        cmd.SetAction(async (pr, ct) => {
            await CliTabularConvert.CsvToXlsxAsync(pr.GetValue(input), pr.GetValue(output)!, CliCsv.Create(), CliXlsx.Create(), ct).ConfigureAwait(false);
        });
        return cmd;
    }
}
