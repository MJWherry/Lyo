using Lyo.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Tools.Postgres;

/// <summary>Headless entry: <c>seed {name} [--via ef|api] [--count N] [--seed N] [--skip] [--replace] [--base-url URL]</c>.</summary>
internal static class SeedCli
{
    public static async Task<int> RunAsync(IServiceProvider sp, string[] args, CancellationToken ct)
    {
        var name = args.Length > 1 ? args[1] : "";
        if (string.IsNullOrWhiteSpace(name) || name.StartsWith('-')) {
            Console.Error.WriteLine("Usage: seed {people|people-api|comic} [--via ef|api] [--count N] [--seed N] [--skip] [--replace] [--base-url URL]");
            return 2;
        }

        var via = ParseVia(args);
        var count = ParseInt(args, "--count") ?? 50;
        var randomSeed = ParseInt(args, "--seed");
        var conflict = args.Contains("--replace", StringComparer.OrdinalIgnoreCase)
            ? SeedConflictMode.Replace
            : args.Contains("--skip", StringComparer.OrdinalIgnoreCase)
                ? SeedConflictMode.SkipIfNotEmpty
                : SeedConflictMode.Append;
        var baseUrl = ParseValue(args, "--base-url");
        var contributors = sp.GetRequiredService<IEnumerable<SeedContributor>>().ToList();
        var contributor = Match(contributors, name);
        if (contributor == null) {
            Console.Error.WriteLine($"Unknown seeder '{name}'. Available: {string.Join(", ", contributors.Select(c => c.Name))}.");
            return 2;
        }

        via ??= contributor.SupportedTransports.HasFlag(SeedTransportKind.Ef) && !contributor.SupportedTransports.HasFlag(SeedTransportKind.Api)
            ? SeedTransportKind.Ef
            : contributor.SupportedTransports.HasFlag(SeedTransportKind.Api) && !contributor.SupportedTransports.HasFlag(SeedTransportKind.Ef)
                ? SeedTransportKind.Api
                : SeedTransportKind.Ef;
        if (contributor.Name.Equals("Comic", StringComparison.OrdinalIgnoreCase) && count == 50 && ParseInt(args, "--count") is null)
            count = 20;

        var options = new SeedOptions { Count = count, RandomSeed = randomSeed, Conflict = conflict };
        var runner = sp.GetRequiredService<ISeedRunner>();
        var conn = sp.GetRequiredService<ConnectionStringProvider>();
        var result = await SeedExecution.RunAsync(contributor, runner, conn, via.Value, options, baseUrl, ct).ConfigureAwait(false);
        if (result.Skipped) {
            Console.WriteLine($"Skipped {result.Contributor} — destination already has rows.");
            return 0;
        }

        if (!result.Success) {
            Console.Error.WriteLine($"Failed {result.Contributor}: {string.Join("; ", result.Errors)}");
            return 1;
        }

        Console.WriteLine($"Seeded {result.Contributor}: {string.Join(", ", result.Counts.Select(kv => $"{kv.Key}={kv.Value}"))}");
        return 0;
    }

    private static SeedContributor? Match(IReadOnlyList<SeedContributor> contributors, string name)
    {
        var exact = contributors.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        var slug = name.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        return contributors.FirstOrDefault(c => c.Name.Replace(" ", "", StringComparison.OrdinalIgnoreCase).Equals(slug, StringComparison.OrdinalIgnoreCase)
            || (name.Equals("people-api", StringComparison.OrdinalIgnoreCase) && c.Name.Contains("API", StringComparison.OrdinalIgnoreCase)));
    }

    private static SeedTransportKind? ParseVia(string[] args)
    {
        var value = ParseValue(args, "--via");
        if (value == null)
            return null;

        if (value.Equals("ef", StringComparison.OrdinalIgnoreCase) || value.Equals("direct", StringComparison.OrdinalIgnoreCase))
            return SeedTransportKind.Ef;
        if (value.Equals("api", StringComparison.OrdinalIgnoreCase))
            return SeedTransportKind.Api;

        throw new SeedException($"Unknown --via '{value}'. Use ef or api.");
    }

    private static int? ParseInt(string[] args, string flag)
    {
        var value = ParseValue(args, flag);
        return int.TryParse(value, out var n) ? n : null;
    }

    private static string? ParseValue(string[] args, string flag)
    {
        for (var i = 0; i < args.Length - 1; i++) {
            if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}
