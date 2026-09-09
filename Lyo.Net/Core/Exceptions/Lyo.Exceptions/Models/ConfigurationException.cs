using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>
/// Thrown when required configuration is missing or wrong (for example a required connection string, key material, or service registration is
/// absent). This is a server-side fault rather than a client error, so it intentionally does not derive from <see cref="HttpException" />.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ConfigurationException : Exception
{
    /// <summary>Name of the missing or invalid setting or section, when known.</summary>
    public string? SettingName { get; }

    /// <summary>Mints a <see cref="ConfigurationException" /> with the default message.</summary>
    public ConfigurationException()
        : base("The application is missing required configuration or is misconfigured.") { }

    /// <summary>Builds a <see cref="ConfigurationException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public ConfigurationException(string message)
        : base(message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public ConfigurationException(string message, Exception? innerException)
        : base(message, innerException) { }

    /// <summary>Builds a <see cref="ConfigurationException" /> that names the offending setting.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="settingName">Name of the missing or invalid setting or section.</param>
    public ConfigurationException(string message, string? settingName)
        : base(message)
        => SettingName = settingName;

    /// <summary>Builds a <see cref="ConfigurationException" /> that names the offending setting and wraps <paramref name="innerException" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="settingName">Name of the missing or invalid setting or section.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public ConfigurationException(string message, string? settingName, Exception? innerException)
        : base(message, innerException)
        => SettingName = settingName;

    /// <inheritdoc />
    public override string ToString() => SettingName != null ? $"{base.ToString()} (Setting: {SettingName})" : base.ToString();
}