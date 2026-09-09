using Lyo.Exceptions;
using Lyo.SystemInformation;

namespace Lyo.Drift.Models;

/// <summary>
/// Stable host facts for drift comparison. Volatile fields (collection time, uptime, free disk, memory used, process lifetime) are never mapped.
/// Arrays are sorted so object-graph diffs walk them by index. Pass <see cref="SystemInfoDriftGroups" /> to control which identity/opt-in groups are filled.
/// </summary>
public sealed class SystemInfoDriftProjection
{
    public string OsDescription { get; set; } = "";
    public string OsPlatform { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string FrameworkDescription { get; set; } = "";
    public string? RuntimeIdentifier { get; set; }
    public string? CpuModel { get; set; }
    public int ProcessorCount { get; set; }
    public string ProcessArchitecture { get; set; } = "";
    public string OsArchitecture { get; set; } = "";
    public long? TotalPhysicalMemoryBytes { get; set; }
    public string HostName { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string? CultureName { get; set; }
    public string? UICultureName { get; set; }
    public string? TimeZoneId { get; set; }
    public DriveFact[] Drives { get; set; } = [];
    public InterfaceFact[] Interfaces { get; set; } = [];
    public MonitorFact[] Monitors { get; set; } = [];
    public EnvVarFact[] Variables { get; set; } = [];

    /// <summary>Maps a live <see cref="SystemInfo" /> onto the identity-only projection.</summary>
    public static SystemInfoDriftProjection From(SystemInfo info)
        => From(info, SystemInfoDriftGroups.Identity);

    /// <summary>Maps a live <see cref="SystemInfo" /> using the given include groups. Hard-omitted volatile fields are never copied.</summary>
    public static SystemInfoDriftProjection From(SystemInfo info, IReadOnlyList<SystemInfoDriftGroup> include)
    {
        ArgumentHelpers.ThrowIfNull(info);
        ArgumentHelpers.ThrowIfNull(include);
        var groups = include.Count == 0 ? SystemInfoDriftGroups.Identity : include;
        var set = new HashSet<SystemInfoDriftGroup>(groups);
        bool Has(SystemInfoDriftGroup group) => set.Contains(group);

        var projection = new SystemInfoDriftProjection();
        if (Has(SystemInfoDriftGroup.Os)) {
            projection.OsDescription = info.Software.OsDescription;
            projection.OsPlatform = info.Software.OsPlatform;
            projection.OsVersion = info.Software.OsVersion;
            projection.FrameworkDescription = info.Software.FrameworkDescription;
            projection.RuntimeIdentifier = info.Software.RuntimeIdentifier;
            projection.ProcessArchitecture = info.Hardware.ProcessArchitecture;
            projection.OsArchitecture = info.Hardware.OsArchitecture;
        }

        if (Has(SystemInfoDriftGroup.Cpu)) {
            projection.CpuModel = info.Hardware.CpuModel;
            projection.ProcessorCount = info.Hardware.ProcessorCount;
        }

        if (Has(SystemInfoDriftGroup.MemoryTotal))
            projection.TotalPhysicalMemoryBytes = info.Hardware.TotalPhysicalMemoryBytes;

        if (Has(SystemInfoDriftGroup.Host)) {
            projection.HostName = info.Network.HostName;
            projection.MachineName = info.Environment.MachineName;
        }

        if (Has(SystemInfoDriftGroup.Locale)) {
            projection.CultureName = info.Environment.CultureName;
            projection.UICultureName = info.Environment.UICultureName;
            projection.TimeZoneId = info.Environment.TimeZoneId;
        }

        if (Has(SystemInfoDriftGroup.Drives)) {
            projection.Drives = info.Hardware.Drives
                .Select(d => new DriveFact { Name = d.Name, Type = d.Type, Format = d.Format, TotalSizeBytes = d.TotalSizeBytes })
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var includeAddresses = Has(SystemInfoDriftGroup.InterfaceAddresses);
        if (Has(SystemInfoDriftGroup.InterfaceNames) || includeAddresses) {
            projection.Interfaces = info.Network.Interfaces
                .Select(i => new InterfaceFact {
                    Name = i.Name,
                    MacAddress = includeAddresses ? i.MacAddress : null,
                    UnicastAddresses = includeAddresses
                        ? i.UnicastAddresses.OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToArray()
                        : []
                })
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (Has(SystemInfoDriftGroup.Monitors)) {
            projection.Monitors = info.Hardware.Monitors
                .Select(m => new MonitorFact {
                    Connector = m.Connector,
                    Model = m.Model,
                    ManufacturerId = m.ManufacturerId,
                    CurrentResolution = m.CurrentResolution,
                    Adapter = m.Adapter,
                    IsPrimary = m.IsPrimary
                })
                .OrderBy(m => m.Connector, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (Has(SystemInfoDriftGroup.Variables)) {
            projection.Variables = info.Environment.Variables
                .Select(kv => new EnvVarFact { Key = kv.Key, Value = kv.Value })
                .OrderBy(v => v.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return projection;
    }

    /// <summary>Stable drive identity: name, type, format, and total size. Free space is omitted.</summary>
    public sealed class DriveFact
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Format { get; set; } = "";
        public long TotalSizeBytes { get; set; }
    }

    /// <summary>NIC identity. MAC and unicast addresses are filled only when <see cref="SystemInfoDriftGroup.InterfaceAddresses" /> is included.</summary>
    public sealed class InterfaceFact
    {
        public string Name { get; set; } = "";
        public string? MacAddress { get; set; }
        public string[] UnicastAddresses { get; set; } = [];
    }

    /// <summary>Monitor identity. Refresh rate is omitted because it can change without a hardware swap.</summary>
    public sealed class MonitorFact
    {
        public string Connector { get; set; } = "";
        public string? Model { get; set; }
        public string? ManufacturerId { get; set; }
        public string? CurrentResolution { get; set; }
        public string? Adapter { get; set; }
        public bool? IsPrimary { get; set; }
    }

    /// <summary>One already-redacted environment variable.</summary>
    public sealed class EnvVarFact
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
