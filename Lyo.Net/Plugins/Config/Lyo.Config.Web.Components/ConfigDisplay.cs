namespace Lyo.Config.Web.Components;

/// <summary>UI helpers for config values in the dashboard (masking encrypted payloads the client should not dump).</summary>
public static class ConfigDisplay
{
    /// <summary>Encrypted values placeholder shown instead of plaintext.</summary>
    public const string Masked = "***";

    /// <summary>Gives <see cref="Masked" /> when <paramref name="encrypted" /> is true; otherwise the JSON (or empty).</summary>
    public static string Json(string? json, bool encrypted) => encrypted ? Masked : json ?? "";
}
