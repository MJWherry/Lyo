namespace Lyo.Common.SystemInformation;

/// <summary>Host name, availability, and network interface details.</summary>
/// <param name="HostName">DNS host name of the machine.</param>
/// <param name="IsNetworkAvailable">Whether any network connection is available.</param>
/// <param name="Interfaces">Details for each network interface on the machine.</param>
public sealed record NetworkInfo(string HostName, bool IsNetworkAvailable, IReadOnlyList<NetworkInterfaceInfo> Interfaces);