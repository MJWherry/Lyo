namespace Lyo.SystemInformation;

/// <summary>Host name, availability, and network-interface facts.</summary>
/// <param name="HostName">DNS host name of this machine.</param>
/// <param name="IsNetworkAvailable">True when any network connection is available.</param>
/// <param name="Interfaces">Facts for each network interface on the machine.</param>
public sealed record NetworkInfo(string HostName, bool IsNetworkAvailable, IReadOnlyList<NetworkInterfaceInfo> Interfaces);