using System.CommandLine;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Cli.Services;
using Lyo.Common;

namespace Lyo.Cli.Commands;

internal static class QueryCommands
{
    public static Command Create()
    {
        var query = new Command("query", "Build and execute Lyo.Query API requests");
        query.Subcommands.Add(CreateBuild());
        query.Subcommands.Add(CreateExec());
        return query;
    }

    private static Command CreateBuild()
    {
        var cmd = new Command("build", "Compose a query request JSON body");
        var modeArg = new Argument<string>("mode") { Description = "concrete | project | root" };
        AddBuildOptions(
            cmd, out var whereOpt, out var whereFileOpt, out var includeOpt, out var sortOpt, out var startOpt, out var amountOpt, out var keyOpt, out var selectOpt,
            out var fromOpt, out var outputOpt);

        cmd.Arguments.Add(modeArg);
        cmd.SetAction(async (pr, ct) => {
            var mode = CliQueryBuilder.ParseMode(pr.GetValue(modeArg)!);
            var whereFile = await ReadOptionalTextAsync(pr.GetValue(whereFileOpt), ct).ConfigureAwait(false);
            var body = CliQueryBuilder.Build(
                mode, pr.GetValue(whereOpt), whereFile, pr.GetValue(includeOpt), pr.GetValue(sortOpt), pr.GetValue(startOpt), pr.GetValue(amountOpt), pr.GetValue(keyOpt),
                pr.GetValue(selectOpt), pr.GetValue(fromOpt));

            await CliIO.WriteTextAsync(pr.GetValue(outputOpt), CliQueryBuilder.Serialize(body), ct).ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command CreateExec()
    {
        var cmd = new Command("exec", "POST a query to a Lyo API");
        var modeArg = new Argument<string>("mode") { Description = "concrete | project | root" };
        var baseUrlOpt = new Option<string?>("--base-url") { Description = "API base URL (or LYO_API_BASE_URL)" };
        var basePathOpt = new Option<string?>("--base-path");
        var routeOpt = new Option<string?>("--route");
        var bodyOpt = new Option<string?>("--body") { Description = "Request JSON file or '-'; default stdin if redirected" };
        var tokenOpt = new Option<string?>("--token") { Description = "Bearer token (or LYO_API_TOKEN)" };
        var headerOpt = new Option<string[]>("--header") { AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => [] };
        var rawOpt = new Option<bool>("--raw");
        AddBuildOptions(
            cmd, out var whereOpt, out var whereFileOpt, out var includeOpt, out var sortOpt, out var startOpt, out var amountOpt, out var keyOpt, out var selectOpt,
            out var fromOpt, out var outputOpt);

        cmd.Arguments.Add(modeArg);
        cmd.Options.Add(baseUrlOpt);
        cmd.Options.Add(basePathOpt);
        cmd.Options.Add(routeOpt);
        cmd.Options.Add(bodyOpt);
        cmd.Options.Add(tokenOpt);
        cmd.Options.Add(headerOpt);
        cmd.Options.Add(rawOpt);
        cmd.SetAction(async (pr, ct) => {
            var mode = CliQueryBuilder.ParseMode(pr.GetValue(modeArg)!);
            var baseUrl = pr.GetValue(baseUrlOpt) ?? Environment.GetEnvironmentVariable("LYO_API_BASE_URL");
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("--base-url or LYO_API_BASE_URL is required.");

            object body;
            var bodyPath = pr.GetValue(bodyOpt);
            var hasBuildFlags = (pr.GetValue(whereOpt)?.Length ?? 0) > 0 || !string.IsNullOrWhiteSpace(pr.GetValue(whereFileOpt)) || (pr.GetValue(includeOpt)?.Length ?? 0) > 0 ||
                (pr.GetValue(sortOpt)?.Length ?? 0) > 0 || pr.GetValue(startOpt) is not null || pr.GetValue(amountOpt) is not null || (pr.GetValue(keyOpt)?.Length ?? 0) > 0 ||
                (pr.GetValue(selectOpt)?.Length ?? 0) > 0 || !string.IsNullOrWhiteSpace(pr.GetValue(fromOpt));

            if (hasBuildFlags && string.IsNullOrWhiteSpace(bodyPath)) {
                var whereFile = await ReadOptionalTextAsync(pr.GetValue(whereFileOpt), ct).ConfigureAwait(false);
                body = CliQueryBuilder.Build(
                    mode, pr.GetValue(whereOpt), whereFile, pr.GetValue(includeOpt), pr.GetValue(sortOpt), pr.GetValue(startOpt), pr.GetValue(amountOpt), pr.GetValue(keyOpt),
                    pr.GetValue(selectOpt), pr.GetValue(fromOpt));
            }
            else {
                var json = await CliIO.ReadAllTextAsync(bodyPath, ct).ConfigureAwait(false);
                body = JsonSerializer.Deserialize<JsonElement>(json, LyoJsonSerializerOptions.Create());
            }

            var token = pr.GetValue(tokenOpt) ?? Environment.GetEnvironmentVariable("LYO_API_TOKEN");
            try {
                var response = await CliQueryExecutor.ExecAsync(
                        mode, baseUrl!, pr.GetValue(basePathOpt), pr.GetValue(routeOpt), body!, token, pr.GetValue(headerOpt), pr.GetValue(rawOpt), ct)
                    .ConfigureAwait(false);

                await CliIO.WriteTextAsync(pr.GetValue(outputOpt), response, ct).ConfigureAwait(false);
            }
            catch (ApiException) {
                Environment.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static void AddBuildOptions(
        Command cmd,
        out Option<string[]> whereOpt,
        out Option<string?> whereFileOpt,
        out Option<string[]> includeOpt,
        out Option<string[]> sortOpt,
        out Option<int?> startOpt,
        out Option<int?> amountOpt,
        out Option<string[]> keyOpt,
        out Option<string[]> selectOpt,
        out Option<string?> fromOpt,
        out Option<string?> outputOpt)
    {
        whereOpt = new("--where") { AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => [], Description = "FIELD:OP:VALUE" };
        whereFileOpt = new("--where-file");
        includeOpt = new("--include") { AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => [] };
        sortOpt = new("--sort") { AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => [], Description = "FIELD[:asc|desc]" };
        startOpt = new("--start");
        amountOpt = new("--amount");
        keyOpt = new("--key") { AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => [] };
        selectOpt = new("--select") { AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => [] };
        fromOpt = new("--from") { Description = "ALIAS:ENTITY (root)" };
        outputOpt = new("--output", "-o");
        cmd.Options.Add(whereOpt);
        cmd.Options.Add(whereFileOpt);
        cmd.Options.Add(includeOpt);
        cmd.Options.Add(sortOpt);
        cmd.Options.Add(startOpt);
        cmd.Options.Add(amountOpt);
        cmd.Options.Add(keyOpt);
        cmd.Options.Add(selectOpt);
        cmd.Options.Add(fromOpt);
        cmd.Options.Add(outputOpt);
    }

    private static async Task<string?> ReadOptionalTextAsync(string? path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return await CliIO.ReadAllTextAsync(path, ct).ConfigureAwait(false);
    }
}