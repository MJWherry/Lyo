using System.Runtime.InteropServices;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Net;

/// <summary>
/// Builds one desktop User-Agent for a browser grammar. The OS token comes from <see cref="RuntimeInformation" /> and <see cref="Environment.OSVersion" />. Callers pass the
/// grammar and browser version only.
/// </summary>
public static class UserAgentGenerator
{
    /// <summary>Chrome and Edge version stamped by current Lyo defaults.</summary>
    public const string ChromeVersion = "140.0.7339.80";

    /// <summary>Firefox version stamped by current Lyo defaults.</summary>
    public const string FirefoxVersion = "133.0";

    /// <summary>Safari version stamped by current Lyo defaults.</summary>
    public const string SafariVersion = "18.0";

    private const string ChromiumEngine = "AppleWebKit/537.36 (KHTML, like Gecko)";
    private const string SafariEngine = "AppleWebKit/605.1.15 (KHTML, like Gecko)";

    /// <summary>Formats a User-Agent for <paramref name="format" /> at <paramref name="version" /> on this host.</summary>
    public static string Generate(UserAgentFormat format, string version)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(version);
        var os = PlatformToken();
        return format switch {
            UserAgentFormat.Chromium => Chromium(os, version),
            UserAgentFormat.Edge => $"{Chromium(os, version)} Edg/{version}",
            UserAgentFormat.Firefox => $"Mozilla/5.0 ({os}; rv:{version}) Gecko/20100101 Firefox/{version}",
            UserAgentFormat.Safari => $"Mozilla/5.0 ({os}) {SafariEngine} Version/{version} Safari/605.1.15",
            var _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private static string Chromium(string os, string version) => $"Mozilla/5.0 ({os}) {ChromiumEngine} Chrome/{version} Safari/537.36";

    private static string PlatformToken()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return WindowsToken();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "Macintosh; Intel Mac OS X 10_15_7";

        return LinuxToken();
    }

    private static string WindowsToken()
    {
        var version = Environment.OSVersion.Version;
        var nt = $"Windows NT {version.Major}.{version.Minor}";
        return RuntimeInformation.OSArchitecture switch {
            Architecture.X86 => $"{nt}; WOW64",
            Architecture.Arm64 => $"{nt}; Win64; ARM64",
            var _ => $"{nt}; Win64; x64"
        };
    }

    private static string LinuxToken()
    {
        var arch = RuntimeInformation.OSArchitecture switch {
            Architecture.Arm64 => "aarch64",
            Architecture.X86 => "i686",
            var _ => "x86_64"
        };

        return $"X11; Linux {arch}";
    }
}