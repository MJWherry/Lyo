namespace Lyo.Common.SystemInformation;

/// <summary>Machine, user, culture, timezone, and environment variable details for the current process.</summary>
/// <param name="MachineName">NetBIOS machine name.</param>
/// <param name="UserName">User name the process is running under.</param>
/// <param name="UserDomainName">Network domain name associated with the current user.</param>
/// <param name="CurrentDirectory">Current working directory of the process.</param>
/// <param name="SystemDirectory">Fully qualified path of the system directory (may be empty on non-Windows platforms).</param>
/// <param name="TempPath">Path of the current user's temporary folder.</param>
/// <param name="CommandLine">Command line of the current process.</param>
/// <param name="CultureName">Name of the current culture (e.g. <c>en-US</c>).</param>
/// <param name="UICultureName">Name of the current UI culture.</param>
/// <param name="TimeZoneId">Identifier of the local time zone.</param>
/// <param name="UtcOffset">Current UTC offset of the local time zone.</param>
/// <param name="SystemUptime">Elapsed time since the system started.</param>
/// <param name="Variables">Environment variables with secret-like values redacted.</param>
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