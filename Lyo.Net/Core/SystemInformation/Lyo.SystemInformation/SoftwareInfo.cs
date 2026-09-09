namespace Lyo.SystemInformation;

/// <summary>Operating system, runtime, and current-process facts.</summary>
/// <param name="OsDescription">Operating system description (for example <c>Linux 6.8.0-136-generic</c>).</param>
/// <param name="OsPlatform">Detected OS platform: <c>Windows</c>, <c>Linux</c>, <c>OSX</c>, or <c>Unknown</c>.</param>
/// <param name="OsVersion">Version string from <see cref="System.Environment.OSVersion" />.</param>
/// <param name="FrameworkDescription">Runtime framework description (for example <c>.NET 10.0.0</c>).</param>
/// <param name="RuntimeIdentifier">Runtime identifier (for example <c>linux-x64</c>) when available; otherwise <see langword="null" />.</param>
/// <param name="ClrVersion">CLR version from <see cref="System.Environment.Version" />.</param>
/// <param name="Is64BitOperatingSystem">True when the operating system is 64-bit.</param>
/// <param name="Is64BitProcess">True when the current process is 64-bit.</param>
/// <param name="IsServerGC">True when the server garbage collector is enabled.</param>
/// <param name="ProcessId">Id of the current process.</param>
/// <param name="ProcessName">Name of the current process.</param>
/// <param name="ProcessStartTimeUtc">UTC time when the current process started.</param>
/// <param name="ProcessUptime">Elapsed time since the current process started.</param>
public sealed record SoftwareInfo(
    string OsDescription,
    string OsPlatform,
    string OsVersion,
    string FrameworkDescription,
    string? RuntimeIdentifier,
    string ClrVersion,
    bool Is64BitOperatingSystem,
    bool Is64BitProcess,
    bool IsServerGC,
    int ProcessId,
    string ProcessName,
    DateTime ProcessStartTimeUtc,
    TimeSpan ProcessUptime);