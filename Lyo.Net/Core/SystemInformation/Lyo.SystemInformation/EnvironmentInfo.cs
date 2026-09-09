namespace Lyo.SystemInformation;

/// <summary>Machine, user, culture, timezone, and environment-variable facts for the current process.</summary>
/// <param name="MachineName">NetBIOS name of the machine.</param>
/// <param name="UserName">User name the process is running as.</param>
/// <param name="UserDomainName">Network domain associated with the current user.</param>
/// <param name="CurrentDirectory">Working directory of the process.</param>
/// <param name="SystemDirectory">Fully qualified system-directory path (may be empty on non-Windows platforms).</param>
/// <param name="TempPath">Path of the current user's temp folder.</param>
/// <param name="CommandLine">Full command line of the current process.</param>
/// <param name="CultureName">Name of the current culture (for example <c>en-US</c>).</param>
/// <param name="UICultureName">Name of the current UI culture for this process.</param>
/// <param name="TimeZoneId">Id of the local time zone.</param>
/// <param name="UtcOffset">Current UTC offset of the local time zone.</param>
/// <param name="SystemUptime">Elapsed time since the machine started.</param>
/// <param name="Variables">Environment variables with secret-like values already redacted.</param>
public sealed record EnvironmentInfo(
    string MachineName,
    string UserName,
    string UserDomainName,
    string CurrentDirectory,
    string SystemDirectory,
    string TempPath,
    string CommandLine,
    string CultureName,
    string UICultureName,
    string TimeZoneId,
    TimeSpan UtcOffset,
    TimeSpan SystemUptime,
    IReadOnlyDictionary<string, string> Variables);