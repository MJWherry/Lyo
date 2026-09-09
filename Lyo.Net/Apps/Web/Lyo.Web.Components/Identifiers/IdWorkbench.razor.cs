using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Identifiers;

public partial class IdWorkbench
{
    private GuidVersion _guidVersion = GuidVersion.V7;
    private int _guidCount = 5;
    private string _guidNamespace = "DNS";
    private string _guidCustomNamespace = string.Empty;
    private string _guidName = string.Empty;
    private List<IdEntry> _guidEntries = [];

    private int _ksuidCount = 5;
    private List<IdEntry> _ksuidEntries = [];

    private int _ulidCount = 5;
    private List<IdEntry> _ulidEntries = [];

    private string _nanoAlphabet = NanoId.DefaultAlphabet;
    private int _nanoSize = NanoId.DefaultSize;
    private int _nanoCount = 5;
    private List<IdEntry> _nanoEntries = [];

    private int _snowMachineId;
    private int _snowCount = 5;
    private List<IdEntry> _snowEntries = [];

    private string _autoType = "int";
    private long _autoStart;
    private int _autoCount = 5;
    private List<IdEntry> _autoEntries = [];

    private void GenerateGuids()
    {
        try {
            _guidEntries = _guidVersion switch {
                GuidVersion.V3 => [GenerateNamedGuid(GuidVersion.V3)],
                GuidVersion.V5 => [GenerateNamedGuid(GuidVersion.V5)],
                var _ => LyoGuid.CreateBulk(_guidVersion, _guidCount).Select(g => new IdEntry(g.ToString(), ExtractGuidTimestamp(g, _guidVersion))).ToList()
            };
        }
        catch (Exception ex) {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private IdEntry GenerateNamedGuid(GuidVersion version)
    {
        var ns = _guidNamespace switch {
            "DNS" => LyoGuid.Namespace.Dns,
            "URL" => LyoGuid.Namespace.Url,
            "OID" => LyoGuid.Namespace.Oid,
            "X500" => LyoGuid.Namespace.X500,
            "Custom" => Guid.TryParse(_guidCustomNamespace, out var custom) ? custom : throw new ArgumentException("Custom namespace is not a valid GUID."),
            var _ => LyoGuid.Namespace.Dns
        };

        var g = version == GuidVersion.V3 ? LyoGuid.CreateV3(ns, _guidName) : LyoGuid.CreateV5(ns, _guidName);
        return new(g.ToString());
    }

    private static string? ExtractGuidTimestamp(Guid g, GuidVersion version)
    {
        if (version is not (GuidVersion.V6 or GuidVersion.V7 or GuidVersion.CombPostgres or GuidVersion.CombSqlServer))
            return null;

        try {
            var ts = LyoGuid.GetTimestamp(g);
            return $"≈ {ts:yyyy-MM-dd HH:mm:ss.fff} UTC";
        }
        catch {
            return null;
        }
    }

    private void GenerateKsuids() => _ksuidEntries = Ksuid.CreateBulk(_ksuidCount).Select(k => new IdEntry(k, $"≈ {Ksuid.GetTimestamp(k):yyyy-MM-dd HH:mm:ss} UTC")).ToList();

    private void GenerateUlids() => _ulidEntries = Ulid.CreateBulk(_ulidCount).Select(u => new IdEntry(u, $"≈ {Ulid.GetTimestamp(u):yyyy-MM-dd HH:mm:ss.fff} UTC")).ToList();

    private void GenerateNanoIds()
    {
        try {
            _nanoEntries = NanoId.CreateBulk(_nanoCount, _nanoAlphabet, _nanoSize).Select(n => new IdEntry(n)).ToList();
        }
        catch (Exception ex) {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private void GenerateSnowflakes()
    {
        try {
            var gen = new SnowflakeGenerator(_snowMachineId);
            _snowEntries = gen.NextBulk(_snowCount).Select(s => new IdEntry(s.Value.ToString(), $"≈ {s.GetTimestampUtc(gen.Layout):yyyy-MM-dd HH:mm:ss.fff} UTC")).ToList();
        }
        catch (Exception ex) {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private void GenerateAutoIncrementIds()
    {
        try {
            _autoEntries = _autoType switch {
                "int" => GenerateAutoIncrementIntEntries(),
                "long" => GenerateAutoIncrementLongEntries(),
                "uint" => GenerateAutoIncrementUIntEntries(),
                "ulong" => GenerateAutoIncrementULongEntries(),
                var _ => throw new ArgumentOutOfRangeException(nameof(_autoType), _autoType, "Unsupported auto-increment type.")
            };
        }
        catch (Exception ex) {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private List<IdEntry> GenerateAutoIncrementIntEntries()
    {
        if (_autoStart < int.MinValue || _autoStart > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(_autoStart), $"Start value must be between {int.MinValue} and {int.MaxValue} for int.");

        var gen = new AutoIncrementIdGenerator<int>((int)_autoStart);
        return Enumerable.Range(0, _autoCount).Select(_ => new IdEntry(gen.Next().ToString(), $"current={gen.Current}")).ToList();
    }

    private List<IdEntry> GenerateAutoIncrementLongEntries()
    {
        var gen = new AutoIncrementIdGenerator<long>(_autoStart);
        return Enumerable.Range(0, _autoCount).Select(_ => new IdEntry(gen.Next().ToString(), $"current={gen.Current}")).ToList();
    }

    private List<IdEntry> GenerateAutoIncrementUIntEntries()
    {
        if (_autoStart < uint.MinValue || _autoStart > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(_autoStart), $"Start value must be between {uint.MinValue} and {uint.MaxValue} for uint.");

        var gen = new AutoIncrementIdGenerator<uint>((uint)_autoStart);
        return Enumerable.Range(0, _autoCount).Select(_ => new IdEntry(gen.Next().ToString(), $"current={gen.Current}")).ToList();
    }

    private List<IdEntry> GenerateAutoIncrementULongEntries()
    {
        if (_autoStart < 0)
            throw new ArgumentOutOfRangeException(nameof(_autoStart), "Start value must be non-negative for ulong.");

        var gen = new AutoIncrementIdGenerator<ulong>((ulong)_autoStart);
        return Enumerable.Range(0, _autoCount).Select(_ => new IdEntry(gen.Next().ToString(), $"current={gen.Current}")).ToList();
    }
}
