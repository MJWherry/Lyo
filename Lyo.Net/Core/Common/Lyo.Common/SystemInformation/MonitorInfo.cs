namespace Lyo.Common.SystemInformation;

/// <summary>Details for a single connected monitor/display. Fields not obtainable on the current platform are <see langword="null" />.</summary>
/// <param name="Connector">Connector or device name (e.g. <c>eDP-1</c>, <c>HDMI-A-1</c> on Linux; <c>\\.\DISPLAY1</c> on Windows).</param>
/// <param name="Model">Monitor model name (from the EDID display descriptor on Linux; the monitor device string on Windows).</param>
/// <param name="ManufacturerId">Three-letter PNP manufacturer code from the EDID (Linux only).</param>
/// <param name="CurrentResolution">Current or preferred resolution (e.g. <c>1920x1200</c>).</param>
/// <param name="RefreshRateHz">Current refresh rate in Hz (Windows only).</param>
/// <param name="Adapter">Display adapter (GPU) name driving the monitor (Windows only).</param>
/// <param name="IsPrimary">Whether this is the primary display (Windows only; <see langword="null" /> when unknown).</param>
public sealed record MonitorInfo(string Connector, string? Model, string? ManufacturerId, string? CurrentResolution, int? RefreshRateHz, string? Adapter, bool? IsPrimary);