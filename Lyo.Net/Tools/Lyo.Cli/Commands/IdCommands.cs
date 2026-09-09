using System.CommandLine;
using Lyo.Cli.Services;

namespace Lyo.Cli.Commands;

internal static class IdCommands
{
    public static Command Create()
    {
        var id = new Command("id", "Generate and parse identifiers (ULID, KSUID, NanoID, GUID, Snowflake)");
        var ulid = CreateSimpleGenerate("ulid", CliIdentifiers.GenerateUlid);
        ulid.Subcommands.Add(CreateParseCommand(CliIdentifiers.ParseUlidTimestamp));
        id.Subcommands.Add(ulid);
        var ksuid = CreateSimpleGenerate("ksuid", CliIdentifiers.GenerateKsuid);
        ksuid.Subcommands.Add(CreateParseCommand(CliIdentifiers.ParseKsuidTimestamp));
        id.Subcommands.Add(ksuid);
        id.Subcommands.Add(CreateNanoId());
        var guid = CreateGuid();
        guid.Subcommands.Add(CreateTimestampCommand());
        id.Subcommands.Add(guid);
        var snow = CreateSnowflake();
        snow.Subcommands.Add(CreateParseCommand(CliIdentifiers.ParseSnowflakeTimestamp));
        id.Subcommands.Add(snow);
        return id;
    }

    private static Command CreateSimpleGenerate(string name, Func<int, IReadOnlyList<string>> gen)
    {
        var cmd = new Command(name, $"Generate {name}");
        AddEmitOptions(cmd, out var countOpt, out var outputOpt, out var copyOpt, out var quietOpt);
        cmd.SetAction(async (pr, ct) => {
            var lines = gen(Math.Max(1, pr.GetValue(countOpt)));
            await CliIdentifiers.EmitAsync(lines, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command CreateNanoId()
    {
        var cmd = new Command("nanoid", "Generate NanoID");
        var sizeOpt = new Option<int>("--size") { DefaultValueFactory = _ => 21 };
        var alphabetOpt = new Option<string?>("--alphabet");
        AddEmitOptions(cmd, out var countOpt, out var outputOpt, out var copyOpt, out var quietOpt);
        cmd.Options.Add(sizeOpt);
        cmd.Options.Add(alphabetOpt);
        cmd.SetAction(async (pr, ct) => {
            var lines = CliIdentifiers.GenerateNanoId(Math.Max(1, pr.GetValue(countOpt)), pr.GetValue(sizeOpt), pr.GetValue(alphabetOpt));
            await CliIdentifiers.EmitAsync(lines, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command CreateGuid()
    {
        var cmd = new Command("guid", "Generate GUID variants");
        foreach (var ver in new[] { "v4", "v6", "v7", "comb-pg", "comb-sql", "v3", "v5" }) {
            var sub = new Command(ver, $"Generate GUID {ver}");
            AddEmitOptions(sub, out var countOpt, out var outputOpt, out var copyOpt, out var quietOpt);
            var nsOpt = new Option<string?>("--ns");
            var nameOpt = new Option<string?>("--name");
            if (ver is "v3" or "v5") {
                nsOpt.Required = true;
                nameOpt.Required = true;
                sub.Options.Add(nsOpt);
                sub.Options.Add(nameOpt);
            }

            sub.SetAction(async (pr, ct) => {
                var version = CliIdentifiers.ParseGuidVersion(ver);
                Guid? ns = null;
                if (ver is "v3" or "v5")
                    ns = CliIdentifiers.ParseNamespace(pr.GetValue(nsOpt)!);

                var lines = CliIdentifiers.GenerateGuid(version, Math.Max(1, pr.GetValue(countOpt)), ns, pr.GetValue(nameOpt));
                await CliIdentifiers.EmitAsync(lines, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
            });

            cmd.Subcommands.Add(sub);
        }

        return cmd;
    }

    private static Command CreateSnowflake()
    {
        var cmd = new Command("snowflake", "Generate snowflake IDs");
        var machineOpt = new Option<int>("--machine") { DefaultValueFactory = _ => 0 };
        AddEmitOptions(cmd, out var countOpt, out var outputOpt, out var copyOpt, out var quietOpt);
        cmd.Options.Add(machineOpt);
        cmd.SetAction(async (pr, ct) => {
            var lines = CliIdentifiers.GenerateSnowflake(Math.Max(1, pr.GetValue(countOpt)), pr.GetValue(machineOpt));
            await CliIdentifiers.EmitAsync(lines, pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command CreateParseCommand(Func<string, string> parse)
    {
        var cmd = new Command("parse", "Parse timestamp from id");
        var idArg = new Argument<string>("id");
        AddEmitOptions(cmd, out var _, out var outputOpt, out var copyOpt, out var quietOpt, false);
        cmd.Arguments.Add(idArg);
        cmd.SetAction(async (pr, ct) => {
            var text = parse(pr.GetValue(idArg)!);
            await CliIdentifiers.EmitAsync([text], pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command CreateTimestampCommand()
    {
        var cmd = new Command("timestamp", "Extract timestamp from v6/v7 GUID");
        var idArg = new Argument<string>("guid");
        AddEmitOptions(cmd, out var _, out var outputOpt, out var copyOpt, out var quietOpt, false);
        cmd.Arguments.Add(idArg);
        cmd.SetAction(async (pr, ct) => {
            var text = CliIdentifiers.ParseGuidTimestamp(pr.GetValue(idArg)!);
            await CliIdentifiers.EmitAsync([text], pr.GetValue(outputOpt), pr.GetValue(copyOpt), pr.GetValue(quietOpt), ct).ConfigureAwait(false);
        });

        return cmd;
    }

    private static void AddEmitOptions(
        Command cmd,
        out Option<int> countOpt,
        out Option<string?> outputOpt,
        out Option<bool> copyOpt,
        out Option<bool> quietOpt,
        bool includeCount = true)
    {
        countOpt = new("--count", "-n") { DefaultValueFactory = _ => 1, Description = "Number of IDs to generate (one per line)" };
        outputOpt = new("--output", "-o");
        copyOpt = new("--copy", "-c") { Description = "Copy result to system clipboard" };
        quietOpt = new("--quiet", "-q") { Description = "Suppress stdout" };
        if (includeCount)
            cmd.Options.Add(countOpt);

        cmd.Options.Add(outputOpt);
        cmd.Options.Add(copyOpt);
        cmd.Options.Add(quietOpt);
    }
}