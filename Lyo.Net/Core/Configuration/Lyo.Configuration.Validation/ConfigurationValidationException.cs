namespace Lyo.Configuration.Validation;

/// <summary>Thrown when configuration validation itself is mis-set, not when values fail their rules (those come back as errors).</summary>
public sealed class ConfigurationValidationException(string message) : Exception(message);
