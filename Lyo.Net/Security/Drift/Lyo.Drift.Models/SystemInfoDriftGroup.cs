namespace Lyo.Drift.Models;

/// <summary>
/// Named field groups for <see cref="SystemInfoDriftProjection" />. Identity groups are the default; extra groups are opt-in.
/// Volatile facts (uptime, free disk, memory used, process lifetime) are never included.
/// </summary>
public enum SystemInfoDriftGroup
{
    /// <summary>OS description, platform, version, framework, RID, and process/OS architecture.</summary>
    Os,

    /// <summary>CPU model and logical processor count.</summary>
    Cpu,

    /// <summary>Total physical RAM. Used/available memory is never projected.</summary>
    MemoryTotal,

    /// <summary>DNS host name and machine name.</summary>
    Host,

    /// <summary>Drive name, type, format, and total size. Free space is never projected.</summary>
    Drives,

    /// <summary>Network interface names only.</summary>
    InterfaceNames,

    /// <summary>Redacted environment variables.</summary>
    Variables,

    /// <summary>NIC MAC and unicast addresses (implies names).</summary>
    InterfaceAddresses,

    /// <summary>Connected monitors (identity fields; refresh rate is omitted).</summary>
    Monitors,

    /// <summary>Culture, UI culture, and time-zone id. UTC offset is omitted because it flaps with DST.</summary>
    Locale
}

/// <summary>Parses agent include lists into <see cref="SystemInfoDriftGroup" /> values.</summary>
public static class SystemInfoDriftGroups
{
    /// <summary>Default identity set: OS, CPU, RAM total, host, drives, NIC names.</summary>
    public static IReadOnlyList<SystemInfoDriftGroup> Identity { get; } = [
        SystemInfoDriftGroup.Os,
        SystemInfoDriftGroup.Cpu,
        SystemInfoDriftGroup.MemoryTotal,
        SystemInfoDriftGroup.Host,
        SystemInfoDriftGroup.Drives,
        SystemInfoDriftGroup.InterfaceNames
    ];

    /// <summary>
    /// Parses configured group names. Null, empty, or all-whitespace lists return <see cref="Identity" />.
    /// Unknown names throw <see cref="ArgumentException" />.
    /// </summary>
    public static IReadOnlyList<SystemInfoDriftGroup> Parse(IEnumerable<string>? names)
    {
        if (names is null)
            return Identity;

        var parsed = new List<SystemInfoDriftGroup>();
        foreach (var raw in names) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            if (!Enum.TryParse<SystemInfoDriftGroup>(raw.Trim(), ignoreCase: true, out var group))
                throw new ArgumentException($"Unknown system-info include group '{raw.Trim()}'.", nameof(names));
            if (!parsed.Contains(group))
                parsed.Add(group);
        }

        return parsed.Count == 0 ? Identity : parsed;
    }
}
