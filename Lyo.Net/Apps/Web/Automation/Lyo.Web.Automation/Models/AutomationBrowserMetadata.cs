using System.Runtime.InteropServices;
using Lyo.Common.Core.Net;

namespace Lyo.Web.Automation.Models;

/// <summary>Grammar and browser version <see cref="UserAgentGenerator" /> stamps for one <see cref="AutomationBrowserKind" />. The record does not store a finished User-Agent.</summary>
/// <param name="Format">Which product token to emit.</param>
/// <param name="Version">Browser version passed to <see cref="UserAgentGenerator.Generate" />.</param>
public sealed record AutomationBrowserUserAgentTemplate(UserAgentFormat Format, string Version);

/// <summary>One install location for <see cref="AutomationBrowserMetadata" />. <see cref="Architecture" /> is set for Windows and null on macOS and Linux.</summary>
/// <param name="OperatingSystem">OS this path belongs to.</param>
/// <param name="Architecture">Process architecture, or null when the path is shared by every architecture on that OS.</param>
/// <param name="Path">Absolute path of the browser executable.</param>
public sealed record AutomationBrowserPath(OSPlatform OperatingSystem, Architecture? Architecture, string Path);

/// <summary>
/// Catalog row for one <see cref="AutomationBrowserKind" />: display name, environment variables checked before install paths, those paths, and the User-Agent template.
/// <see cref="AutomationBrowserDetector" /> reads this. The row has no lookup methods.
/// </summary>
/// <param name="Kind">Browser this row describes.</param>
/// <param name="Name">Display name (for example <c>Google Chrome</c>).</param>
/// <param name="EnvironmentVariables">Env vars checked in order. A value that names an existing file wins over <see cref="Paths" />.</param>
/// <param name="Paths">Well-known install locations. The detector picks the first existing file for the current OS.</param>
/// <param name="UserAgent">Format and version used to generate a User-Agent for this browser on the host OS.</param>
public sealed record AutomationBrowserMetadata(
    AutomationBrowserKind Kind,
    string Name,
    IReadOnlyList<string> EnvironmentVariables,
    IReadOnlyList<AutomationBrowserPath> Paths,
    AutomationBrowserUserAgentTemplate UserAgent);