namespace Lyo.SystemInformation;

/// <summary>Aggregate host snapshot: hardware, software/runtime, network, and environment facts.</summary>
/// <param name="Hardware">Hardware facts (CPU, memory, drives).</param>
/// <param name="Software">Operating system, runtime, and process facts.</param>
/// <param name="Network">Host name and network-interface facts.</param>
/// <param name="Environment">Machine, user, culture, timezone, and (redacted) environment variables.</param>
/// <param name="CollectedAtUtc">UTC timestamp when the snapshot was taken.</param>
public sealed record SystemInfo(HardwareInfo Hardware, SoftwareInfo Software, NetworkInfo Network, EnvironmentInfo Environment, DateTime CollectedAtUtc);