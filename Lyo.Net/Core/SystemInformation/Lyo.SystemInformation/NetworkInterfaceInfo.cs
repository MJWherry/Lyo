namespace Lyo.SystemInformation;

/// <summary>Facts for one network interface.</summary>
/// <param name="Name">Interface name (for example <c>eth0</c>).</param>
/// <param name="Description">Human-readable interface description.</param>
/// <param name="InterfaceType">Interface type (for example <c>Ethernet</c>, <c>Wireless80211</c>, <c>Loopback</c>).</param>
/// <param name="OperationalStatus">Operational status (for example <c>Up</c>, <c>Down</c>).</param>
/// <param name="SpeedBitsPerSecond">Link speed in bits per second, or <see langword="null" /> when the platform does not report it.</param>
/// <param name="MacAddress">Physical (MAC) address, or <see langword="null" /> when unavailable (for example loopback).</param>
/// <param name="UnicastAddresses">Unicast IP addresses assigned on this interface.</param>
/// <param name="GatewayAddresses">Gateway addresses configured on the interface.</param>
/// <param name="DnsAddresses">DNS server addresses configured on the interface.</param>
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