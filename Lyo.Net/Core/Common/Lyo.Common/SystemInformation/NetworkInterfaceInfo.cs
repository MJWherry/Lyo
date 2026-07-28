namespace Lyo.Common.SystemInformation;

/// <summary>Details for a single network interface.</summary>
/// <param name="Name">Interface name (e.g. <c>eth0</c>).</param>
/// <param name="Description">Interface description.</param>
/// <param name="InterfaceType">Interface type (e.g. <c>Ethernet</c>, <c>Wireless80211</c>, <c>Loopback</c>).</param>
/// <param name="OperationalStatus">Operational status (e.g. <c>Up</c>, <c>Down</c>).</param>
/// <param name="SpeedBitsPerSecond">Link speed in bits per second, or <see langword="null" /> when not reported by the platform.</param>
/// <param name="MacAddress">Physical (MAC) address, or <see langword="null" /> when unavailable (e.g. loopback).</param>
/// <param name="UnicastAddresses">Unicast IP addresses assigned to the interface.</param>
/// <param name="GatewayAddresses">Gateway addresses configured for the interface.</param>
/// <param name="DnsAddresses">DNS server addresses configured for the interface.</param>
public sealed record NetworkInterfaceInfo(
    string Name,
    string Description,
    string InterfaceType,
    string OperationalStatus,
    long? SpeedBitsPerSecond,
    string? MacAddress,
    IReadOnlyList<string> UnicastAddresses,
    IReadOnlyList<string> GatewayAddresses,
    IReadOnlyList<string> DnsAddresses);