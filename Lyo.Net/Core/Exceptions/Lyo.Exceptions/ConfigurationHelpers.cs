using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lyo.Exceptions.Models;
#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.

namespace Lyo.Exceptions;

/// <summary>Guards for configuration validation that throw <see cref="ConfigurationException" />.</summary>
/// <remarks>
/// Unlike <see cref="OperationHelpers" /> (invalid runtime state) and <see cref="ArgumentHelpers" /> (caller contract violations), this type signals missing or invalid
/// application configuration — required connection strings, key material, options values, or service registrations. Overloads optionally capture the caller&apos;s value expression
/// (via <see cref="CallerArgumentExpressionAttribute" />), surfaced as the <see cref="ConfigurationException.SettingName" /> of thrown exceptions.
/// </remarks>
public static class ConfigurationHelpers
{
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowConfiguration(string message, string? settingName) => throw new ConfigurationException(message, settingName);

    /// <summary>Throws <see cref="ConfigurationException" /> when the condition is true.</summary>
    /// <param name="condition">Condition under test. When true, <see cref="ConfigurationException" /> is thrown.</param>
    /// <param name="message">Message used for the exception.</param>
    /// <param name="settingName">Name of the configuration setting or section that is missing or invalid, when provided.</param>
    /// <exception cref="ConfigurationException">Thrown if the condition is true.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string message, string? settingName = null)
    {
        if (condition)
            ThrowConfiguration(message, settingName);
    }

    /// <summary>Throws <see cref="ConfigurationException" /> when the value is null.</summary>
    /// <param name="value">Configured value under test.</param>
    /// <param name="message">Exception message. When null, a default message is used.</param>
    /// <param name="settingName">Omitted: caller expression for <paramref name="value" />. Surfaced as <see cref="ConfigurationException.SettingName" />.</param>
    /// <exception cref="ConfigurationException">Thrown if the value is null.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] object? value, string? message = null, [CallerArgumentExpression("value")] string? settingName = null)
    {
        if (value == null)
            ThrowConfiguration(message ?? $"Required configuration value '{settingName ?? "unknown"}' is not set.", settingName);
    }

    /// <summary>Throws <see cref="ConfigurationException" /> when the string is null or whitespace.</summary>
    /// <param name="value">Configured string under test.</param>
    /// <param name="message">Exception message. When null, a default message is used.</param>
    /// <param name="settingName">Omitted: caller expression for <paramref name="value" />. Surfaced as <see cref="ConfigurationException.SettingName" />.</param>
    /// <exception cref="ConfigurationException">Thrown if the value is null or whitespace.</exception>
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