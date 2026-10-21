namespace Lyo.SystemInformation;

/// <summary>
/// Best-effort power-policy facts. User-facing profile, CPU scaling, and AC vs battery are reported separately because they are independent knobs; fields the current
/// platform cannot supply are <see langword="null" />.
/// </summary>
/// <param name="Profile">
/// User-facing power mode. Linux: ACPI <c>platform_profile</c> when present. Windows: overlay scheme friendly name when an overlay is in effect, otherwise the
/// active power scheme name.
/// </param>
/// <param name="CpuGovernor">Linux cpufreq <c>scaling_governor</c> (unique values joined when CPUs disagree). Null on Windows and other platforms.</param>
/// <param name="EnergyPerformancePreference">Linux cpufreq <c>energy_performance_preference</c> (unique values joined when CPUs disagree). Null elsewhere.</param>
/// <param name="SchemeId">Windows active power-scheme GUID. Null on Linux and other platforms.</param>
/// <param name="OnAcPower">
/// True when a mains adapter is online (Linux: <c>power_supply</c> type <c>Mains</c>; Windows: <c>GetSystemPowerStatus</c>). Null when the platform does not
/// expose AC vs battery.
/// </param>
public sealed record PowerPolicyInfo(string? Profile, string? CpuGovernor, string? EnergyPerformancePreference, string? SchemeId, bool? OnAcPower);