using System.ComponentModel;
using System.Reflection;
using Lyo.Common.Core.Attributes;

namespace Lyo.Common.Metadata.Extensions;

/// <summary>Helpers that read enum display metadata (<see cref="DescriptionAttribute" />, <see cref="StringValueAttribute" />) and test flag bits.</summary>
public static class EnumMetadataExtensions
{
    /// <summary>Returns the <see cref="StringValueAttribute" /> text for <paramref name="value" />, or the member name when the attribute is missing.</summary>
    /// <param name="value">The enum member.</param>
    /// <returns>The attribute string, or <c>ToString()</c> of the member when no attribute is present.</returns>
    public static string GetStringValue(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<StringValueAttribute>();
        return attribute?.Value ?? value.ToString();
    }

    /// <summary>True when the underlying integral value has exactly one bit set (a single flag on a <see cref="FlagsAttribute" />-style enum).</summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum member to test.</param>
    /// <returns><see langword="true" /> when the underlying value is non-zero and a power of two; otherwise <see langword="false" />.</returns>
    public static bool IsSingleFlag<T>(this T value)
        where T : Enum
    {
        var intValue = Convert.ToInt64(value);
        return intValue != 0 && (intValue & (intValue - 1)) == 0;
    }

    /// <param name="value">The enum member.</param>
    /// <typeparam name="T">The enum type.</typeparam>
    extension<T>(T? value)
        where T : Enum
    {
        /// <summary>Reads <see cref="DescriptionAttribute" /> text for the enum field, or the member name when the attribute is missing.</summary>
        /// <returns>The description, or <see langword="null" /> when the receiver is <see langword="null" />.</returns>
        public string? GetDescription()
        {
            if (value is null)
                return null;

            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        /// <summary>Reads <see cref="StringValueAttribute" /> text for the enum member, or the member name when the attribute is missing.</summary>
        /// <returns>The attribute string, or the member name; <see langword="null" /> when the receiver is <see langword="null" />.</returns>
        public string? GetStringValue()
        {
            if (value is null)
                return null;

            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<StringValueAttribute>();
            return attribute?.Value ?? value.ToString();
        }
    }
}