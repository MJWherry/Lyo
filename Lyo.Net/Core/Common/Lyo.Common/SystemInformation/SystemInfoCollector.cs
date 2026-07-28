using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Lyo.Common.Extensions;

namespace Lyo.Common.SystemInformation;

/// <summary>
/// Gathers hardware, software/runtime, network, and environment information about the host using only built-in platform APIs. Individual items that cannot be read on the
/// current platform are skipped or reported as <see langword="null" /> rather than failing the whole snapshot.
/// </summary>
public static class SystemInfoCollector
{
    private const string RedactedValue = "********";
    private static readonly Regex SensitiveVariableRegex = new(@"(?i)(secret|token|password|passwd|pwd|api[_-]?key|connection[_-]?string|credential|auth)", RegexOptions.Compiled);

    /// <summary>Collects a full snapshot of hardware, software, network, and environment information.</summary>
    /// <returns>A populated <see cref="SystemInfo" /> with <see cref="SystemInfo.CollectedAtUtc" /> set to the current UTC time.</returns>
    public static SystemInfo Collect() => new(GetHardwareInfo(), GetSoftwareInfo(), GetNetworkInfo(), GetEnvironmentInfo(), DateTime.UtcNow);

    /// <summary>Collects hardware details: logical processor count, CPU model, architectures, total physical memory, ready drives, and connected monitors.</summary>
    public static HardwareInfo GetHardwareInfo()
        => new(
            Environment.ProcessorCount, GetCpuModel(), RuntimeInformation.ProcessArchitecture.ToString(), RuntimeInformation.OSArchitecture.ToString(),
            GetTotalPhysicalMemoryBytes(), GetDrives(), GetMonitors());

    /// <summary>Collects operating system, runtime, and current-process details.</summary>
    public static SoftwareInfo GetSoftwareInfo()
    {
        using var process = Process.GetCurrentProcess();
        var startTimeUtc = TryGetValue(() => process.StartTime.ToUniversalTime()) ?? default;
        return new(
            RuntimeInformation.OSDescription, GetOsPlatform(), Environment.OSVersion.VersionString, RuntimeInformation.FrameworkDescription,
#if NET10_0_OR_GREATER
            RuntimeInformation.RuntimeIdentifier,
#else
            null,
#endif
            Environment.Version.ToString(), Environment.Is64BitOperatingSystem, Environment.Is64BitProcess, GCSettings.IsServerGC,
#if NET10_0_OR_GREATER
            Environment.ProcessId,
#else
            process.Id,
#endif
            TryGet(() => process.ProcessName) ?? string.Empty, startTimeUtc, startTimeUtc == default ? TimeSpan.Zero : DateTime.UtcNow - startTimeUtc);
    }

    /// <summary>Collects the host name, network availability, and details for each network interface.</summary>
    public static NetworkInfo GetNetworkInfo()
        => new(TryGet(Dns.GetHostName) ?? Environment.MachineName, TryGetValue(NetworkInterface.GetIsNetworkAvailable) ?? false, GetNetworkInterfaces());

    /// <summary>Collects machine, user, culture, timezone, uptime, and environment variable details. Secret-like variable values are redacted.</summary>
    public static EnvironmentInfo GetEnvironmentInfo()
        => new(
            Environment.MachineName, TryGet(() => Environment.UserName) ?? string.Empty, TryGet(() => Environment.UserDomainName) ?? string.Empty,
            TryGet(() => Environment.CurrentDirectory) ?? string.Empty, TryGet(() => Environment.SystemDirectory) ?? string.Empty, TryGet(Path.GetTempPath) ?? string.Empty,
            Environment.CommandLine, CultureInfo.CurrentCulture.Name, CultureInfo.CurrentUICulture.Name, TimeZoneInfo.Local.Id, TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now),
#if NET10_0_OR_GREATER
            TimeSpan.FromMilliseconds(Environment.TickCount64),
#else
            TimeSpan.FromMilliseconds(Environment.TickCount & int.MaxValue),
#endif
            GetEnvironmentVariables());

    private static string? GetCpuModel()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            return TryGet(() => {
                foreach (var line in File.ReadLines("/proc/cpuinfo")) {
                    if (!line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var separatorIndex = line.IndexOf(':');
                    if (separatorIndex >= 0)
                        return line.Substring(separatorIndex + 1).Trim();
                }

                return null;
            });
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TryGet(() => Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"));

        return null;
    }

    private static long? GetTotalPhysicalMemoryBytes()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            var memTotalBytes = TryGetValue(ReadLinuxMemTotalBytes);
            if (memTotalBytes is > 0)
                return memTotalBytes;
        }

#if NET10_0_OR_GREATER
        var available = TryGetValue(() => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes);
        if (available is > 0)
            return available;
#endif
        return null;
    }

    private static long ReadLinuxMemTotalBytes()
    {
        foreach (var line in File.ReadLines("/proc/meminfo")) {
            // Format: "MemTotal:       65505124 kB"
            if (!line.StartsWith("MemTotal", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && long.TryParse(parts[1], out var kilobytes))
                return kilobytes * 1024;

            return 0;
        }

        return 0;
    }

    private static IReadOnlyList<DriveSpaceInfo> GetDrives()
    {
        var drives = new List<DriveSpaceInfo>();
        foreach (var drive in TryGet(DriveInfo.GetDrives) ?? []) {
            var info = TryGet(() => drive.IsReady
                ? new DriveSpaceInfo(drive.Name, drive.DriveType.ToString(), drive.DriveFormat, drive.TotalSize, drive.AvailableFreeSpace)
                : null);

            if (info is not null)
                drives.Add(info);
        }

        return drives;
    }

    private static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return TryGet(GetLinuxMonitors) ?? [];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TryGet(GetWindowsMonitors) ?? [];

        return [];
    }

    private static IReadOnlyList<MonitorInfo> GetLinuxMonitors()
    {
        var monitors = new List<MonitorInfo>();
        foreach (var connectorDirectory in Directory.EnumerateDirectories("/sys/class/drm", "card*-*")) {
            var info = TryGet(() => {
                var status = TryGet(() => File.ReadAllText(Path.Combine(connectorDirectory, "status")).Trim());
                if (!string.Equals(status, "connected", StringComparison.OrdinalIgnoreCase))
                    return null;

                var directoryName = Path.GetFileName(connectorDirectory);
                var separatorIndex = directoryName.IndexOf('-');
                var connector = separatorIndex >= 0 ? directoryName.Substring(separatorIndex + 1) : directoryName;
                var resolution = TryGet(() => File.ReadLines(Path.Combine(connectorDirectory, "modes")).FirstOrDefault());
                var edid = TryGet(() => File.ReadAllBytes(Path.Combine(connectorDirectory, "edid")));
                return new MonitorInfo(
                    connector, EdidParser.GetModelName(edid), EdidParser.GetManufacturerId(edid), resolution.IsNullOrEmpty() ? null : resolution, null, null, null);
            });

            if (info is not null)
                monitors.Add(info);
        }

        return monitors;
    }

    private static IReadOnlyList<MonitorInfo> GetWindowsMonitors()
    {
        var monitors = new List<MonitorInfo>();
        for (var adapterIndex = 0u;; adapterIndex++) {
            var adapter = NativeDisplayMethods.NewDisplayDevice();
            if (!NativeDisplayMethods.EnumDisplayDevices(null, adapterIndex, ref adapter, 0))
                break;

            if ((adapter.StateFlags & NativeDisplayMethods.DisplayDeviceAttachedToDesktop) == 0)
                continue;

            var info = TryGet(() => {
                var monitor = NativeDisplayMethods.NewDisplayDevice();
                var monitorName = NativeDisplayMethods.EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0) ? monitor.DeviceString : null;
                string? resolution = null;
                int? refreshRate = null;
                var devMode = new NativeDisplayMethods.DevMode { dmSize = (ushort)Marshal.SizeOf<NativeDisplayMethods.DevMode>() };
                if (NativeDisplayMethods.EnumDisplaySettings(adapter.DeviceName, NativeDisplayMethods.EnumCurrentSettings, ref devMode)) {
                    resolution = $"{devMode.dmPelsWidth}x{devMode.dmPelsHeight}";
                    refreshRate = devMode.dmDisplayFrequency > 1 ? (int)devMode.dmDisplayFrequency : null;
                }

                return new MonitorInfo(
                    adapter.DeviceName, monitorName, null, resolution, refreshRate, adapter.DeviceString,
                    (adapter.StateFlags & NativeDisplayMethods.DisplayDevicePrimaryDevice) != 0);
            });

            if (info is not null)
                monitors.Add(info);
        }

        return monitors;
    }

    private static string GetOsPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return nameof(OSPlatform.Windows);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return nameof(OSPlatform.Linux);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return nameof(OSPlatform.OSX);

        return "Unknown";
    }

    private static IReadOnlyList<NetworkInterfaceInfo> GetNetworkInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>();
        foreach (var nic in TryGet(NetworkInterface.GetAllNetworkInterfaces) ?? []) {
            var info = TryGet(() => {
                var ipProperties = TryGet(nic.GetIPProperties);
                var macAddress = TryGet(() => nic.GetPhysicalAddress().ToString());
                var speed = TryGetValue(() => nic.Speed);
                return new NetworkInterfaceInfo(
                    nic.Name, nic.Description, nic.NetworkInterfaceType.ToString(), nic.OperationalStatus.ToString(), speed is > 0 ? speed : null,
                    macAddress.IsNullOrEmpty() ? null : FormatMacAddress(macAddress!), ToAddressList(ipProperties, p => p.UnicastAddresses.Select(a => a.Address)),
                    ToAddressList(ipProperties, p => p.GatewayAddresses.Select(a => a.Address)), ToAddressList(ipProperties, p => p.DnsAddresses));
            });

            if (info is not null)
                interfaces.Add(info);
        }

        return interfaces;
    }

    private static IReadOnlyList<string> ToAddressList(IPInterfaceProperties? properties, Func<IPInterfaceProperties, IEnumerable<IPAddress>> selector)
    {
        if (properties is null)
            return [];

        return TryGet(() => selector(properties).Select(a => a.ToString()).ToList()) ?? [];
    }

    private static string FormatMacAddress(string rawMacAddress)
    {
        // PhysicalAddress.ToString() returns contiguous hex (e.g. "AABBCCDDEEFF"); insert dashes for readability.
        if (rawMacAddress.Length == 0 || rawMacAddress.Length % 2 != 0)
            return rawMacAddress;

        var pairs = new string[rawMacAddress.Length / 2];
        for (var i = 0; i < pairs.Length; i++)
            pairs[i] = rawMacAddress.Substring(i * 2, 2);

        return string.Join("-", pairs);
    }

    private static IReadOnlyDictionary<string, string> GetEnvironmentVariables()
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var raw = TryGet(Environment.GetEnvironmentVariables);
        if (raw is null)
            return variables;

        foreach (DictionaryEntry entry in raw) {
            var key = entry.Key.ToString();
            if (key is null)
                continue;

            var value = entry.Value?.ToString() ?? string.Empty;
            variables[key] = SensitiveVariableRegex.IsMatch(key) ? RedactedValue : value;
        }

        return variables;
    }

    private static T? TryGet<T>(Func<T?> getter)
        where T : class
    {
        try {
            return getter();
        }
        catch {
            return null;
        }
    }

    private static T? TryGetValue<T>(Func<T> getter)
        where T : struct
    {
        try {
            return getter();
        }
        catch {
            return null;
        }
    }

    /// <summary>user32 display enumeration interop; only invoked behind a Windows platform guard.</summary>
    private static class NativeDisplayMethods
    {
        internal const uint DisplayDeviceAttachedToDesktop = 0x1;
        internal const uint DisplayDevicePrimaryDevice = 0x4;
        internal const int EnumCurrentSettings = -1;

        internal static DisplayDevice NewDisplayDevice() => new() { cb = Marshal.SizeOf<DisplayDevice>() };

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DevMode lpDevMode);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DisplayDevice
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public uint StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DevMode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public ushort dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency;
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }
    }
}