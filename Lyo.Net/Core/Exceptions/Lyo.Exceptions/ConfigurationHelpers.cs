using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lyo.Exceptions.Models;
#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.

namespace Lyo.Exceptions;

/// <summary>Helper methods for configuration validation that throw <see cref="ConfigurationException" />.</summary>
/// <remarks>
/// Unlike <see cref="OperationHelpers" /> (invalid runtime state) and <see cref="ArgumentHelpers" /> (caller contract violations), this type signals missing or invalid
/// application configuration — required connection strings, key material, options values, or service registrations. Overloads optionally capture the caller&apos;s value
/// expression (via <see cref="CallerArgumentExpressionAttribute" />), surfaced as the <see cref="ConfigurationException.SettingName" /> of thrown exceptions.
/// </remarks>
public static class ConfigurationHelpers
{
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowConfiguration(string message, string? settingName) => throw new ConfigurationException(message, settingName);

    /// <summary>Throws a <see cref="ConfigurationException" /> if the condition is true.</summary>
    /// <param name="condition">The condition to check. If true, a <see cref="ConfigurationException" /> is thrown.</param>
    /// <param name="message">The error message.</param>
    /// <param name="settingName">The name of the configuration setting or section that is missing or invalid, when provided.</param>
    /// <exception cref="ConfigurationException">Thrown when condition is true.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string message, string? settingName = null)
    {
        if (condition)
            ThrowConfiguration(message, settingName);
    }

    /// <summary>Throws a <see cref="ConfigurationException" /> if the value is null.</summary>
    /// <param name="value">The configured value to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <param name="settingName">Omitted: caller expression for <paramref name="value" />. Surfaced as <see cref="ConfigurationException.SettingName" />.</param>
    /// <exception cref="ConfigurationException">Thrown when value is null.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] object? value, string? message = null, [CallerArgumentExpression("value")] string? settingName = null)
    {
        if (value == null)
            ThrowConfiguration(message ?? $"Required configuration value '{settingName ?? "unknown"}' is not set.", settingName);
    }

    /// <summary>Throws a <see cref="ConfigurationException" /> if the string is null or whitespace.</summary>
    /// <param name="value">The configured string to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <param name="settingName">Omitted: caller expression for <paramref name="value" />. Surfaced as <see cref="ConfigurationException.SettingName" />.</param>
    /// <exception cref="ConfigurationException">Thrown when value is null or whitespace.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? value, string? message = null, [CallerArgumentExpression("value")] string? settingName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            ThrowConfiguration(message ?? $"Required configuration value '{settingName ?? "unknown"}' is not set or is whitespace.", settingName);
    }
}
