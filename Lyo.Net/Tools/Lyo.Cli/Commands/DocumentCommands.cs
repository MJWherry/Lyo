using System.CommandLine;
using System.Text;
using Lyo.Cli.Services;
using Lyo.Json;
using Lyo.Toml;
using Lyo.Xml;
using Lyo.Yaml;

namespace Lyo.Cli.Commands;

internal static class DocumentCommands
{
    public static IEnumerable<Command> Create()
    {
        var json = new JsonService();
        var yaml = new YamlService();
        var xml = new XmlService();
        var toml = new TomlService();
        yield return Format(
            "json", "JSON convert", ("to-yaml", "Convert JSON to YAML", text => yaml.ConvertFromJson(text)), ("to-xml", "Convert JSON to XML", text => xml.ConvertFromJson(text)),
            ("to-toml", "Convert JSON to TOML", text => toml.ConvertFromJson(text)));

        yield return Format(
            "yaml", "YAML convert", ("to-json", "Convert YAML to JSON", text => yaml.ConvertToJson(text)),
            ("to-xml", "Convert YAML to XML", text => xml.ConvertFromJson(yaml.ConvertToJson(text))),
            ("to-toml", "Convert YAML to TOML", text => toml.ConvertFromJson(yaml.ConvertToJson(text))));

        yield return Format(
            "xml", "XML convert", ("to-json", "Convert XML to JSON", text => xml.ConvertToJson(text)),
            ("to-yaml", "Convert XML to YAML", text => yaml.ConvertFromJson(xml.ConvertToJson(text))),
            ("to-toml", "Convert XML to TOML", text => toml.ConvertFromJson(xml.ConvertToJson(text))));

        yield return Format(
            "toml", "TOML convert", ("to-json", "Convert TOML to JSON", text => toml.ConvertToJson(text)),
            ("to-yaml", "Convert TOML to YAML", text => yaml.ConvertFromJson(toml.ConvertToJson(text))),
            ("to-xml", "Convert TOML to XML", text => xml.ConvertFromJson(toml.ConvertToJson(text))));
    }

    private static Command Format(string name, string description, params (string Name, string Description, Func<string, string> Convert)[] verbs)
    {
        var command = new Command(name, description);
        foreach (var verb in verbs)
            command.Subcommands.Add(ConvertCommand(verb.Name, verb.Description, verb.Convert));

        return command;
    }

    private static Command ConvertCommand(string name, string description, Func<string, string> convert)
    {
        var command = new Command(name, description);
        var input = new Argument<string?>("input") { Arity = ArgumentArity.ZeroOrOne, Description = "Input path. Omit or pass - to read stdin." };
        var output = new Option<string?>("--output", "-o") { Description = "Output path. Omit or pass - to write stdout." };
        command.Arguments.Add(input);
        command.Options.Add(output);
        command.SetAction(async (parse, ct) => {
            var text = await ReadInputAsync(parse.GetValue(input), ct).ConfigureAwait(false);
            await CliIO.WriteTextAsync(parse.GetValue(output), convert(text), ct).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<string> ReadInputAsync(string? path, CancellationToken ct)
    {
        var (stream, leaveOpen, _) = CliIO.OpenInput(path);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }
}