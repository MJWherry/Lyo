using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime;
using System.Runtime.InteropServices;
#if NET10_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Text.RegularExpressions;
using Lyo.Common.Core.Extensions;

namespace Lyo.SystemInformation;

/// <summary>
/// Collects hardware, software/runtime, network, and environment facts about the host using only built-in platform APIs. Items that cannot be read on the current
/// platform are skipped or reported as <see langword="null" /> rather than failing the whole snapshot.
/// </summary>
public static class SystemInfoCollector
{
    private const string RedactedValue = "********";
    private static readonly Regex SensitiveVariableRegex = new(@"(?i)(secret|token|password|passwd|pwd|api[_-]?key|connection[_-]?string|credential|auth)", RegexOptions.Compiled);

    /// <summary>Builds a full snapshot of hardware, software, network, and environment facts.</summary>
    /// <returns>A filled <see cref="SystemInfo" /> whose <see cref="SystemInfo.CollectedAtUtc" /> is the current UTC time.</returns>
    public static SystemInfo Collect() => new(GetHardwareInfo(), GetSoftwareInfo(), GetNetworkInfo(), GetEnvironmentInfo(), DateTime.UtcNow);

    /// <summary>
    /// Reads hardware details: logical processor count, CPU model, architectures, total physical memory, ready drives, connected monitors, audio endpoints, installed printers,
    /// and power policy.
    /// </summary>
    public static HardwareInfo GetHardwareInfo()
        => new(
            Environment.ProcessorCount, GetCpuModel(), RuntimeInformation.ProcessArchitecture.ToString(), RuntimeInformation.OSArchitecture.ToString(),
            GetTotalPhysicalMemoryBytes(), GetDrives(), GetMonitors(), GetAudioDevices(), GetPrinters(), GetPowerPolicy());

    /// <summary>Reads operating system, runtime, and current-process details.</summary>
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

    /// <summary>Reads the host name, network availability, and details for each network interface.</summary>
    public static NetworkInfo GetNetworkInfo()
        => new(TryGet(Dns.GetHostName) ?? Environment.MachineName, TryGetValue(NetworkInterface.GetIsNetworkAvailable) ?? false, GetNetworkInterfaces());

    /// <summary>Reads machine, user, culture, timezone, uptime, and environment variables. Secret-like values are redacted.</summary>
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
            // Line shape: "MemTotal:       65505124 kB"
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

    private static IReadOnlyList<AudioDeviceInfo> GetAudioDevices()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return TryGet(GetLinuxAudioDevices) ?? [];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TryGet(NativeAudioMethods.GetDevices) ?? [];

        return [];
    }

    private static IReadOnlyList<AudioDeviceInfo> GetLinuxAudioDevices()
        => AlsaProcParser.Parse(TryGet(() => File.ReadAllText("/proc/asound/cards")), TryGet(() => File.ReadAllText("/proc/asound/pcm")));

    private static IReadOnlyList<PrinterInfo> GetPrinters()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return TryGet(GetLinuxPrinters) ?? [];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TryGet(NativePrinterMethods.GetDevices) ?? [];

        return [];
    }

    private static IReadOnlyList<PrinterInfo> GetLinuxPrinters()
        => CupsPrintersConfParser.Parse(
            TryGet(() => File.ReadAllText("/etc/cups/printers.conf")),
            TryGet(() => CupsPrintersConfParser.ReadDefaultName(File.ReadAllText("/etc/cups/lpoptions"))));

    private static PowerPolicyInfo GetPowerPolicy()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return TryGet(GetLinuxPowerPolicy) ?? new(null, null, null, null, null);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TryGet(GetWindowsPowerPolicy) ?? new(null, null, null, null, null);

        return new(null, null, null, null, null);
    }

    private static PowerPolicyInfo GetLinuxPowerPolicy()
        => new(
            ReadTrimmedFile("/sys/firmware/acpi/platform_profile"), ReadCpuSysfsUnique("scaling_governor"), ReadCpuSysfsUnique("energy_performance_preference"), null,
            ReadLinuxOnAcPower());

    private static PowerPolicyInfo GetWindowsPowerPolicy()
    {
        string? schemeId = null;
        string? profile = null;
        var scheme = TryGetWindowsActiveScheme();
        if (scheme is { } active) {
            schemeId = active.ToString();
            profile = ReadPowerFriendlyName(active);
            var overlay = TryGetWindowsEffectiveOverlay();
            if (overlay is { } overlayGuid && overlayGuid != Guid.Empty) {
                var overlayName = ReadPowerFriendlyName(overlayGuid);
                if (!string.IsNullOrEmpty(overlayName))
                    profile = overlayName;
            }
        }

        return new(profile, null, null, schemeId, ReadWindowsOnAcPower());
    }

    private static string? ReadTrimmedFile(string path)
        => TryGet(() => {
            var text = File.ReadAllText(path).Trim();
            return text.Length == 0 ? null : text;
        });

    private static string? ReadCpuSysfsUnique(string fileName)
        => TryGet(() => {
            var values = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var cpuDirectory in Directory.EnumerateDirectories("/sys/devices/system/cpu")) {
                var name = Path.GetFileName(cpuDirectory);
                if (name.Length < 4 || !name.StartsWith("cpu", StringComparison.Ordinal) || !char.IsDigit(name[3]))
                    continue;

                var value = ReadTrimmedFile(Path.Combine(cpuDirectory, "cpufreq", fileName));
                if (value is not null)
                    values.Add(value);
            }

            return values.Count == 0 ? null : string.Join(", ", values);
        });

    private static bool? ReadLinuxOnAcPower()
    {
        try {
            var sawMains = false;
            var anyOnline = false;
            foreach (var supplyDirectory in Directory.EnumerateDirectories("/sys/class/power_supply")) {
                var type = ReadTrimmedFile(Path.Combine(supplyDirectory, "type"));
                if (!string.Equals(type, "Mains", StringComparison.OrdinalIgnoreCase))
                    continue;

                sawMains = true;
                if (ReadTrimmedFile(Path.Combine(supplyDirectory, "online")) == "1")
                    anyOnline = true;
            }

            return sawMains ? anyOnline : null;
        }
        catch {
            return null;
        }
    }

    private static Guid? TryGetWindowsActiveScheme()
    {
        var ptr = IntPtr.Zero;
        try {
            if (NativePowerMethods.PowerGetActiveScheme(IntPtr.Zero, out ptr) != 0 || ptr == IntPtr.Zero)
                return null;

            return Marshal.PtrToStructure<Guid>(ptr);
        }
        finally {
            if (ptr != IntPtr.Zero)
                NativePowerMethods.LocalFree(ptr);
        }
    }

    private static Guid? TryGetWindowsEffectiveOverlay()
    {
        try {
            if (NativePowerMethods.PowerGetEffectiveOverlayScheme(out var overlay) != 0)
                return null;
            return overlay;
        }
        catch (EntryPointNotFoundException) {
            return null;
        }
        catch (DllNotFoundException) {
            return null;
        }
    }

    private static string? ReadPowerFriendlyName(Guid scheme)
    {
        uint size = 0;
        var schemeCopy = scheme;
        NativePowerMethods.PowerReadFriendlyName(IntPtr.Zero, ref schemeCopy, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref size);
        if (size == 0)
            return null;

        var buffer = Marshal.AllocHGlobal((int)size);
        try {
            schemeCopy = scheme;
            if (NativePowerMethods.PowerReadFriendlyName(IntPtr.Zero, ref schemeCopy, IntPtr.Zero, IntPtr.Zero, buffer, ref size) != 0)
                return null;

            var name = Marshal.PtrToStringUni(buffer)?.Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }
        finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool? ReadWindowsOnAcPower()
    {
        if (!NativePowerMethods.GetSystemPowerStatus(out var status))
            return null;

        return status.ACLineStatus switch {
            0 => false,
            1 => true,
            _ => null
        };
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
        // PhysicalAddress.ToString() returns contiguous hex (for example "AABBCCDDEEFF"); insert dashes for readability.
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

    /// <summary>user32 display-enumeration interop; called only behind a Windows platform guard.</summary>
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

    /// <summary>powrprof / kernel32 power-policy interop; called only behind a Windows platform guard.</summary>
    private static class NativePowerMethods
    {
        [DllImport("powrprof.dll")]
        internal static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
        internal static extern uint PowerReadFriendlyName(
            IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr Buffer, ref uint BufferSize);

        [DllImport("powrprof.dll")]
        internal static extern uint PowerGetEffectiveOverlayScheme(out Guid EffectiveOverlayPolicyGuid);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        internal static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SystemPowerStatus
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }
    }

    /// <summary>WASAPI endpoint enumeration; called only behind a Windows platform guard.</summary>
#if NET10_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    private static class NativeAudioMethods
    {
        private const int Render = 0;
        private const int Capture = 1;
        private const int ConsoleRole = 0;
        private const uint Active = 0x1;
        private const ushort VariantTypeString = 31;
        private const ushort VariantTypeUInt32 = 19;

        private static readonly PropertyKey FriendlyNameKey = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);
        private static readonly PropertyKey DeviceDescriptionKey = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 2);
        private static readonly PropertyKey FormFactorKey = new(new Guid("1da5d803-d492-4edd-8c23-e0c0ffee7f0e"), 0);

        internal static IReadOnlyList<AudioDeviceInfo> GetDevices()
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            try {
                var defaultIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                RememberDefault(enumerator, Render, defaultIds);
                RememberDefault(enumerator, Capture, defaultIds);
                var devices = new List<AudioDeviceInfo>();
                CollectEndpoints(enumerator, Render, AudioDeviceInfo.Playback, defaultIds, devices);
                CollectEndpoints(enumerator, Capture, AudioDeviceInfo.Capture, defaultIds, devices);
                return devices;
            }
            finally {
                Marshal.ReleaseComObject(enumerator);
            }
        }

        private static void RememberDefault(IMMDeviceEnumerator enumerator, int dataFlow, HashSet<string> defaultIds)
        {
            IMMDevice? device = null;
            try {
                enumerator.GetDefaultAudioEndpoint(dataFlow, ConsoleRole, out device);
                var id = ReadId(device);
                if (id is { Length: > 0 })
                    defaultIds.Add(id);
            }
            catch {
                // No console default for this direction.
            }
            finally {
                if (device is not null)
                    Marshal.ReleaseComObject(device);
            }
        }

        private static void CollectEndpoints(IMMDeviceEnumerator enumerator, int dataFlow, string kind, HashSet<string> defaultIds, List<AudioDeviceInfo> devices)
        {
            IMMDeviceCollection? collection = null;
            try {
                enumerator.EnumAudioEndpoints(dataFlow, Active, out collection);
                collection.GetCount(out var count);
                for (uint index = 0; index < count; index++) {
                    IMMDevice? device = null;
                    try {
                        collection.Item(index, out device);
                        var info = ReadDevice(device, kind, defaultIds);
                        if (info is not null)
                            devices.Add(info);
                    }
                    catch {
                        // Skip an endpoint whose properties cannot be read.
                    }
                    finally {
                        if (device is not null)
                            Marshal.ReleaseComObject(device);
                    }
                }
            }
            finally {
                if (collection is not null)
                    Marshal.ReleaseComObject(collection);
            }
        }

        private static AudioDeviceInfo? ReadDevice(IMMDevice device, string kind, HashSet<string> defaultIds)
        {
            var id = ReadId(device);
            var name = ReadString(device, FriendlyNameKey);
            if (name is not { Length: > 0 })
                return null;

            bool? isDefault = id is null ? null : defaultIds.Contains(id);
            return new AudioDeviceInfo(name, kind, ReadString(device, DeviceDescriptionKey), ReadFormFactor(device), isDefault);
        }

        private static string? ReadId(IMMDevice device)
        {
            var idPointer = IntPtr.Zero;
            try {
                device.GetId(out idPointer);
                return Marshal.PtrToStringUni(idPointer);
            }
            finally {
                if (idPointer != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(idPointer);
            }
        }

        private static string? ReadString(IMMDevice device, PropertyKey key)
        {
            var value = ReadProperty(device, key);
            try {
                return value.VariantType == VariantTypeString && value.PointerValue != IntPtr.Zero ? Marshal.PtrToStringUni(value.PointerValue) : null;
            }
            finally {
                PropVariantClear(ref value);
            }
        }

        private static string? ReadFormFactor(IMMDevice device)
        {
            var value = ReadProperty(device, FormFactorKey);
            try {
                return value.VariantType == VariantTypeUInt32 ? FormatFormFactor(value.UIntValue) : null;
            }
            finally {
                PropVariantClear(ref value);
            }
        }

        private static PropVariant ReadProperty(IMMDevice device, PropertyKey key)
        {
            device.OpenPropertyStore(0, out var store);
            try {
                store.GetValue(ref key, out var value);
                return value;
            }
            finally {
                Marshal.ReleaseComObject(store);
            }
        }

        private static string? FormatFormFactor(uint value)
            => value switch {
                0 => "RemoteNetworkDevice",
                1 => "Speakers",
                2 => "LineLevel",
                3 => "Headphones",
                4 => "Microphone",
                5 => "Headset",
                6 => "Handset",
                7 => "UnknownDigitalPassthrough",
                8 => "Spdif",
                9 => "Hdmi",
                10 => "Unknown",
                _ => null
            };

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            void EnumAudioEndpoints(int dataFlow, uint stateMask, out IMMDeviceCollection devices);

            void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
        }

        [ComImport]
        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            void GetCount(out uint count);

            void Item(uint index, out IMMDevice device);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            void Activate(ref Guid interfaceId, uint classContext, IntPtr activationParams, out IntPtr instance);

            void OpenPropertyStore(uint access, out IPropertyStore properties);

            void GetId(out IntPtr endpointId);
        }

        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint count);

            void GetAt(uint index, out PropertyKey key);

            void GetValue(ref PropertyKey key, out PropVariant value);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            internal Guid FormatId;
            internal uint PropertyId;

            internal PropertyKey(Guid formatId, uint propertyId)
            {
                FormatId = formatId;
                PropertyId = propertyId;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct PropVariant
        {
            [FieldOffset(0)]
            internal ushort VariantType;

            [FieldOffset(8)]
            internal IntPtr PointerValue;

            [FieldOffset(8)]
            internal uint UIntValue;
        }
    }

    /// <summary>winspool printer enumeration; called only behind a Windows platform guard.</summary>
#if NET10_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    private static class NativePrinterMethods
    {
        private const uint EnumLocal = 0x2;
        private const uint EnumConnections = 0x4;
        private const uint DefaultAttribute = 0x4;

        internal static IReadOnlyList<PrinterInfo> GetDevices()
        {
            const uint flags = EnumLocal | EnumConnections;
            _ = EnumPrinters(flags, null, 2, IntPtr.Zero, 0, out var needed, out _);
            if (needed == 0)
                return [];

            var buffer = Marshal.AllocHGlobal((int)needed);
            try {
                if (!EnumPrinters(flags, null, 2, buffer, needed, out _, out var returned) || returned == 0)
                    return [];

                var printers = new List<PrinterInfo>((int)returned);
                var stride = Marshal.SizeOf<PrinterInfo2>();
                for (var index = 0; index < (int)returned; index++) {
                    var raw = Marshal.PtrToStructure(IntPtr.Add(buffer, index * stride), typeof(PrinterInfo2));
                    if (raw is not PrinterInfo2 info)
                        continue;

                    var name = ReadString(info.PrinterName);
                    if (name is not { Length: > 0 })
                        continue;

                    printers.Add(new PrinterInfo(
                        name, ReadString(info.DriverName), ReadString(info.PortName), (info.Attributes & DefaultAttribute) != 0, FormatStatus(info.Status)));
                }

                return printers;
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string? ReadString(IntPtr pointer)
            => pointer == IntPtr.Zero ? null : Marshal.PtrToStringUni(pointer);

        private static string? FormatStatus(uint status)
        {
            if (status == 0)
                return null;

            var names = new List<string>();
            Add(0x00000001, "Paused");
            Add(0x00000002, "Error");
            Add(0x00000004, "PendingDeletion");
            Add(0x00000008, "PaperJam");
            Add(0x00000010, "PaperOut");
            Add(0x00000020, "ManualFeed");
            Add(0x00000040, "PaperProblem");
            Add(0x00000080, "Offline");
            Add(0x00000100, "IoActive");
            Add(0x00000200, "Busy");
            Add(0x00000400, "Printing");
            Add(0x00000800, "OutputBinFull");
            Add(0x00001000, "NotAvailable");
            Add(0x00002000, "Waiting");
            Add(0x00004000, "Processing");
            Add(0x00008000, "Initializing");
            Add(0x00010000, "WarmingUp");
            Add(0x00020000, "TonerLow");
            Add(0x00040000, "NoToner");
            Add(0x00080000, "PagePunt");
            Add(0x00100000, "UserIntervention");
            Add(0x00200000, "OutOfMemory");
            Add(0x00400000, "DoorOpen");
            Add(0x00800000, "ServerUnknown");
            Add(0x01000000, "PowerSave");
            return names.Count == 0 ? status.ToString(CultureInfo.InvariantCulture) : string.Join(", ", names);

            void Add(uint flag, string name)
            {
                if ((status & flag) != 0)
                    names.Add(name);
            }
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EnumPrinters(uint flags, string? name, uint level, IntPtr buffer, uint bufferSize, out uint bytesNeeded, out uint returned);

        [StructLayout(LayoutKind.Sequential)]
        private struct PrinterInfo2
        {
            internal IntPtr ServerName;
            internal IntPtr PrinterName;
            internal IntPtr ShareName;
            internal IntPtr PortName;
            internal IntPtr DriverName;
            internal IntPtr Comment;
            internal IntPtr Location;
            internal IntPtr DevMode;
            internal IntPtr SepFile;
            internal IntPtr PrintProcessor;
            internal IntPtr Datatype;
            internal IntPtr Parameters;
            internal IntPtr SecurityDescriptor;
            internal uint Attributes;
            internal uint Priority;
            internal uint DefaultPriority;
            internal uint StartTime;
            internal uint UntilTime;
            internal uint Status;
            internal uint JobCount;
            internal uint AveragePagesPerMinute;
        }
    }
}