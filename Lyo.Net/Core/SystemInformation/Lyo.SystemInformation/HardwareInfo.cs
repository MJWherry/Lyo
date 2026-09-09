namespace Lyo.SystemInformation;

/// <summary>Hardware facts about the host system.</summary>
/// <param name="ProcessorCount">Logical processors available to the process.</param>
/// <param name="CpuModel">CPU model when readable (Linux: <c>/proc/cpuinfo</c>; Windows: <c>PROCESSOR_IDENTIFIER</c>); otherwise <see langword="null" />.</param>
/// <param name="ProcessArchitecture">Processor architecture of the current process (for example <c>X64</c>, <c>Arm64</c>).</param>
/// <param name="OsArchitecture">Processor architecture reported for the operating system.</param>
/// <param name="TotalPhysicalMemoryBytes">Total physical memory in bytes when readable; otherwise <see langword="null" />.</param>
/// <param name="Drives">Ready drives with size and free-space facts.</param>
/// <param name="Monitors">Connected monitors (Linux via <c>/sys/class/drm</c>, Windows via user32; empty on other platforms).</param>
public sealed record HardwareInfo(
    int ProcessorCount,
    string? CpuModel,
    string ProcessArchitecture,
    string OsArchitecture,
    long? TotalPhysicalMemoryBytes,
    IReadOnlyList<DriveSpaceInfo> Drives,
    IReadOnlyList<MonitorInfo> Monitors);