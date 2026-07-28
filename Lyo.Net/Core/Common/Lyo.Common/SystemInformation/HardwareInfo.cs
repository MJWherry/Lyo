namespace Lyo.Common.SystemInformation;

/// <summary>Hardware details of the host system.</summary>
/// <param name="ProcessorCount">Number of logical processors available to the process.</param>
/// <param name="CpuModel">CPU model name when obtainable (Linux: <c>/proc/cpuinfo</c>; Windows: <c>PROCESSOR_IDENTIFIER</c>), otherwise <see langword="null" />.</param>
/// <param name="ProcessArchitecture">Processor architecture of the current process (e.g. <c>X64</c>, <c>Arm64</c>).</param>
/// <param name="OsArchitecture">Processor architecture of the operating system.</param>
/// <param name="TotalPhysicalMemoryBytes">Total physical memory in bytes when obtainable, otherwise <see langword="null" />.</param>
/// <param name="Drives">Ready drives with size and free-space information.</param>
/// <param name="Monitors">Connected monitors/displays (Linux via <c>/sys/class/drm</c>, Windows via user32; empty on other platforms).</param>
public sealed record HardwareInfo(
    int ProcessorCount,
    string? CpuModel,
    string ProcessArchitecture,
    string OsArchitecture,
    long? TotalPhysicalMemoryBytes,
    IReadOnlyList<DriveSpaceInfo> Drives,
    IReadOnlyList<MonitorInfo> Monitors);