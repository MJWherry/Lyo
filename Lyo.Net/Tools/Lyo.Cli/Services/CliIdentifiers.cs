using Lyo.Common.Enums;
using Lyo.Common.Identifiers;
using Lyo.Exceptions;

namespace Lyo.Cli.Services;

/// <summary>ID generation and parse helpers over <see cref="Lyo.Common.Identifiers" />.</summary>
internal static class CliIdentifiers
{
    public static IReadOnlyList<string> GenerateUlid(int count) => count <= 1 ? [Ulid.Create()] : Ulid.CreateBulk(count);

    public static IReadOnlyList<string> GenerateKsuid(int count) => count <= 1 ? [Ksuid.Create()] : Ksuid.CreateBulk(count);

    public static IReadOnlyList<string> GenerateNanoId(int count, int size, string? alphabet)
    {
        if (!string.IsNullOrEmpty(alphabet))
            return count <= 1 ? [NanoId.Create(alphabet, size)] : NanoId.CreateBulk(count, alphabet, size);

        return count <= 1 ? [NanoId.Create(size)] : NanoId.CreateBulk(count, size);
    }

    public static IReadOnlyList<string> GenerateGuid(GuidVersion version, int count, Guid? ns, string? name)
    {
        if (version is GuidVersion.V3 or GuidVersion.V5) {
            ArgumentHelpers.ThrowIf(ns is null, "--ns is required for guid v3/v5.");
            ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(name), "--name is required for guid v3/v5.");
            var list = new List<string>(count);
            for (var i = 0; i < count; i++) {
                var g = version == GuidVersion.V3 ? LyoGuid.CreateV3(ns!.Value, name!) : LyoGuid.CreateV5(ns!.Value, name!);
                list.Add(g.ToString("D"));
            }

            return list;
        }

        if (count <= 1)
            return [LyoGuid.Create(version).ToString("D")];

        return LyoGuid.CreateBulk(version, count).Select(g => g.ToString("D")).ToArray();
    }

    public static IReadOnlyList<string> GenerateSnowflake(int count, int machineId)
    {
        var gen = machineId == 0 ? SnowflakeGenerator.Shared : new(machineId);
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
            list.Add(gen.Next().ToString());

        return list;
    }

    public static string ParseUlidTimestamp(string id) => Ulid.GetTimestamp(id).UtcDateTime.ToString("O");

    public static string ParseKsuidTimestamp(string id) => Ksuid.GetTimestamp(id).UtcDateTime.ToString("O");

    public static string ParseGuidTimestamp(string id)
    {
        ArgumentHelpers.ThrowIf(!Guid.TryParse(id, out var g), $"Invalid GUID: {id}");
        return LyoGuid.GetTimestamp(g).UtcDateTime.ToString("O");
    }

    public static string ParseSnowflakeTimestamp(string id)
    {
        ArgumentHelpers.ThrowIf(!long.TryParse(id, out var value), $"Invalid snowflake id: {id}");
        var snow = Snowflake.FromInt64(value);
        return snow.GetTimestampUtc(SnowflakeGenerator.Shared.Layout).UtcDateTime.ToString("O");
    }

    public static GuidVersion ParseGuidVersion(string name)
        => name.Trim().ToLowerInvariant() switch {
            "v3" or "3" => GuidVersion.V3,
            "v4" or "4" => GuidVersion.V4,
            "v5" or "5" => GuidVersion.V5,
            "v6" or "6" => GuidVersion.V6,
            "v7" or "7" => GuidVersion.V7,
            "comb-pg" or "combpg" or "comb-postgres" => GuidVersion.CombPostgres,
            "comb-sql" or "combsql" or "comb-sqlserver" => GuidVersion.CombSqlServer,
            var _ => throw new ArgumentException($"Unknown guid version '{name}'. Use v3, v4, v5, v6, v7, comb-pg, or comb-sql.")
        };

    public static Guid ParseNamespace(string ns)
        => ns.Trim().ToLowerInvariant() switch {
            "dns" => LyoGuid.Namespace.Dns,
            "url" => LyoGuid.Namespace.Url,
            "oid" => LyoGuid.Namespace.Oid,
            "x500" => LyoGuid.Namespace.X500,
            var _ when Guid.TryParse(ns, out var g) => g,
            var _ => throw new ArgumentException($"Unknown namespace '{ns}'. Use dns, url, oid, x500, or a GUID.")
        };

    public static Task EmitAsync(IReadOnlyList<string> lines, string? output, bool copy, bool quiet, CancellationToken ct) => CliIO.EmitTextAsync(lines, output, copy, quiet, ct);
}