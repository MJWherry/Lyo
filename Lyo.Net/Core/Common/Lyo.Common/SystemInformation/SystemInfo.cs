namespace Lyo.Common.SystemInformation;

/// <summary>Aggregate snapshot of the host system: hardware, software/runtime, network, and environment information.</summary>
/// <param name="Hardware">Hardware details (CPU, memory, drives).</param>
/// <param name="Software">Operating system, runtime, and process details.</param>
/// <param name="Network">Host name and network interface details.</param>
/// <param name="Environment">Machine/user/culture/timezone details and (redacted) environment variables.</param>
/// <param name="CollectedAtUtc">UTC timestamp at which the snapshot was collected.</param>
public sealed record SystemInfo(HardwareInfo Hardware, SoftwareInfo Software, NetworkInfo Network, EnvironmentInfo Environment, DateTime CollectedAtUtc);