using System.CommandLine;
using System.Text;
using Lyo.Cli.Commands;

// ExcelDataReader (xlsx) needs legacy code pages (e.g. 1252).
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var root = new RootCommand("Lyo command-line tools — crypt, enc, compress, hash, id, query, csv, xlsx");
root.Subcommands.Add(CryptCommands.Create());
foreach (var c in CompressCommands.Create())
    root.Subcommands.Add(c);

root.Subcommands.Add(EncCommands.Create());
foreach (var c in HashCommands.Create())
    root.Subcommands.Add(c);

root.Subcommands.Add(IdCommands.Create());
root.Subcommands.Add(QueryCommands.Create());
root.Subcommands.Add(CsvCommands.Create());
root.Subcommands.Add(XlsxCommands.Create());
try {
    return await root.Parse(args).InvokeAsync();
}
catch (Exception ex) {
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}