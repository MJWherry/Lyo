using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>
/// Exception thrown when the application is missing required configuration or is misconfigured (e.g. a required connection string, key material, or service registration is
/// absent). Represents a server-side fault rather than a client error, so it intentionally does not derive from <see cref="HttpException" />.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ConfigurationException : Exception
{
    /// <summary>Gets the name of the configuration setting or section that is missing or invalid, if known.</summary>
    public string? SettingName { get; }

    /// <summary>Initializes a new instance of the <see cref="ConfigurationException" /> class.</summary>
    public ConfigurationException()
        : base("The application is missing required configuration or is misconfigured.") { }

    /// <summary>Initializes a new instance of the <see cref="ConfigurationException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="ConfigurationException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception? innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="ConfigurationException" /> class with the offending setting name.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="settingName">The name of the configuration setting or section that is missing or invalid.</param>
    public ConfigurationException(string message, string? settingName)
        : base(message)
        => SettingName = settingName;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationException" /> class with the offending setting name and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="settingName">The name of the configuration setting or section that is missing or invalid.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, string? settingName, Exception? innerException)
        : base(message, innerException)
        => SettingName = settingName;

    public override string ToString() => SettingName != null ? $"{base.ToString()} (Setting: {SettingName})" : base.ToString();
}
