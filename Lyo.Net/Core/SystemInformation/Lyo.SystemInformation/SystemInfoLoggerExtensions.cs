using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.SystemInformation;

/// <summary>
/// <see cref="ILogger" /> helpers that write <see cref="SystemInfo" /> snapshots (or individual sections) as structured log entries. Every entry uses the caller-supplied
/// <see cref="LogLevel" />; nothing is formatted or logged when that level is disabled.
/// </summary>
public static class SystemInfoLoggerExtensions
{
    /// <param name="logger">The logger that receives the entries.</param>
    extension(ILogger logger)
    {
        /// <summary>Writes all four sections of <paramref name="info" /> (hardware, software, network, environment) at <paramref name="level" />.</summary>
        /// <param name="info">The system-information snapshot to write.</param>
        /// <param name="level">The level used for every entry.</param>
        public void LogSystemInfo(SystemInfo info, LogLevel level)
        {
            ArgumentHelpers.ThrowIfNull(logger);
            ArgumentHelpers.ThrowIfNull(info);
            if (!logger.IsEnabled(level))
                return;

            logger.LogHardwareInfo(info.Hardware, level);
            logger.LogSoftwareInfo(info.Software, level);
            logger.LogNetworkInfo(info.Network, level);
            logger.LogEnvironmentInfo(info.Environment, level);
        }

        /// <summary>Writes a hardware summary line plus one line per drive at <paramref name="level" />.</summary>
        /// <param name="hardware">The hardware facts to write.</param>
        /// <param name="level">The level used for every entry.</param>
        public void LogHardwareInfo(HardwareInfo hardware, LogLevel level)
        {
            ArgumentHelpers.ThrowIfNull(logger);
            ArgumentHelpers.ThrowIfNull(hardware);
            if (!logger.IsEnabled(level))
                return;

            logger.Log(
                level, "Hardware: {CpuModel} | {ProcessorCount} logical cores | {ProcessArchitecture} process on {OsArchitecture} OS | {TotalPhysicalMemoryBytes} bytes RAM",
                hardware.CpuModel ?? "Unknown CPU", hardware.ProcessorCount, hardware.ProcessArchitecture, hardware.OsArchitecture, hardware.TotalPhysicalMemoryBytes);

            foreach (var drive in hardware.Drives) {
                logger.Log(
                    level, "Drive {DriveName} ({DriveFormat}, {DriveType}): {AvailableFreeSpaceBytes} free of {TotalSizeBytes} bytes", drive.Name, drive.Format, drive.Type,
                    drive.AvailableFreeSpaceBytes, drive.TotalSizeBytes);
            }

            foreach (var monitor in hardware.Monitors) {
                logger.Log(
                    level, "Monitor {Connector} ({ManufacturerId}, {CurrentResolution}): {MonitorModel}", monitor.Connector, monitor.ManufacturerId ?? "unknown",
                    monitor.CurrentResolution ?? "unknown", monitor.Model ?? "Unknown model");
            }
        }

        /// <summary>Writes an operating-system, runtime, and process summary line at <paramref name="level" />.</summary>
        /// <param name="software">The software facts to write.</param>
        /// <param name="level">The level used for every entry.</param>
        public void LogSoftwareInfo(SoftwareInfo software, LogLevel level)
        {
            ArgumentHelpers.ThrowIfNull(logger);
            ArgumentHelpers.ThrowIfNull(software);
            if (!logger.IsEnabled(level))
                return;

            logger.Log(
                level,
                "Software: {OsDescription} | {FrameworkDescription} | RID {RuntimeIdentifier} | CLR {ClrVersion} | {ProcessBitness} process | ServerGC {IsServerGC} | PID {ProcessId} ({ProcessName}), up {ProcessUptime}",
                software.OsDescription, software.FrameworkDescription, software.RuntimeIdentifier ?? "unknown", software.ClrVersion, software.Is64BitProcess ? "64-bit" : "32-bit",
                software.IsServerGC, software.ProcessId, software.ProcessName, software.ProcessUptime);
        }

        /// <summary>Writes a network summary line plus one line per interface (with addresses) at <paramref name="level" />.</summary>
        /// <param name="network">The network facts to write.</param>
        /// <param name="level">The level used for every entry.</param>
        public void LogNetworkInfo(NetworkInfo network, LogLevel level)
        {
            ArgumentHelpers.ThrowIfNull(logger);
            ArgumentHelpers.ThrowIfNull(network);
            if (!logger.IsEnabled(level))
                return;

            logger.Log(
                level, "Network: host {HostName} | network available {IsNetworkAvailable} | {InterfaceCount} interfaces", network.HostName, network.IsNetworkAvailable,
                network.Interfaces.Count);

            foreach (var nic in network.Interfaces) {
                logger.Log(
                    level,
                    "Interface {InterfaceName} ({InterfaceType}, {OperationalStatus}, {SpeedBitsPerSecond} bps, MAC {MacAddress}): IP [{UnicastAddresses}], gateway [{GatewayAddresses}], DNS [{DnsAddresses}]",
                    nic.Name, nic.InterfaceType, nic.OperationalStatus, nic.SpeedBitsPerSecond, nic.MacAddress ?? "none", string.Join(", ", nic.UnicastAddresses),
                    string.Join(", ", nic.GatewayAddresses), string.Join(", ", nic.DnsAddresses));
            }
        }

        /// <summary>Writes an environment summary line plus the (already redacted) environment variables at <paramref name="level" />.</summary>
        /// <param name="environment">The environment facts to write.</param>
        /// <param name="level">The level used for every entry.</param>
        public void LogEnvironmentInfo(EnvironmentInfo environment, LogLevel level)
        {
            ArgumentHelpers.ThrowIfNull(logger);
            ArgumentHelpers.ThrowIfNull(environment);
            if (!logger.IsEnabled(level))
                return;

            logger.Log(
                level,
                "Environment: machine {MachineName} | user {UserDomainName}\\{UserName} | cwd {CurrentDirectory} | culture {CultureName} | tz {TimeZoneId} (UTC{UtcOffset}) | system uptime {SystemUptime}",
                environment.MachineName, environment.UserDomainName, environment.UserName, environment.CurrentDirectory, environment.CultureName, environment.TimeZoneId,
                environment.UtcOffset, environment.SystemUptime);

            logger.Log(
                level, "Environment variables: {EnvironmentVariables}",
                string.Join(", ", environment.Variables.OrderBy(v => v.Key, StringComparer.OrdinalIgnoreCase).Select(v => $"{v.Key}={v.Value}")));
        }
    }
}