namespace Lyo.SystemInformation;

/// <summary>Facts for one connected monitor. Fields the current platform cannot supply are <see langword="null" />.</summary>
/// <param name="Connector">Connector or device name (for example <c>eDP-1</c>, <c>HDMI-A-1</c> on Linux; <c>\\.\DISPLAY1</c> on Windows).</param>
/// <param name="Model">Monitor model (EDID display descriptor on Linux; monitor device string on Windows).</param>
/// <param name="ManufacturerId">Three-letter PNP manufacturer code from EDID (Linux only).</param>
/// <param name="CurrentResolution">Current or preferred resolution (for example <c>1920x1200</c>).</param>
/// <param name="RefreshRateHz">Current refresh rate in hertz (Windows only).</param>
/// <param name="Adapter">Display adapter (GPU) driving the monitor (Windows only).</param>
/// <param name="IsPrimary">True when this is the primary display (Windows only; <see langword="null" /> when unknown).</param>
public sealed record MonitorInfo(string Connector, string? Model, string? ManufacturerId, string? CurrentResolution, int? RefreshRateHz, string? Adapter, bool? IsPrimary);