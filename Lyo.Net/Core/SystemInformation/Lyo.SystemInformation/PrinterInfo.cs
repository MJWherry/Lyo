namespace Lyo.SystemInformation;

/// <summary>Facts for one installed printer. <see cref="Status" /> is a live state and is omitted from drift projections.</summary>
/// <param name="Name">Printer name.</param>
/// <param name="Driver">Driver name when the platform reports one; otherwise <see langword="null" />.</param>
/// <param name="Port">Port or device URI (Windows port name, or the CUPS <c>DeviceURI</c>).</param>
/// <param name="IsDefault">True when this is the default printer.</param>
/// <param name="Status">Live status (CUPS state, or Windows status flags). Null when the printer is ready or the platform does not report a state.</param>
public sealed record PrinterInfo(string Name, string? Driver, string? Port, bool IsDefault, string? Status);