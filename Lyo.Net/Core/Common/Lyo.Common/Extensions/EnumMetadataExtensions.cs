using System.ComponentModel;
using System.Reflection;
using Lyo.Common.Attributes;
using Lyo.Common.Enums;
using Lyo.Common.Records;

namespace Lyo.Common.Extensions;

/// <summary>Extension methods for reading enum display metadata (<see cref="DescriptionAttribute"/>, <see cref="StringValueAttribute"/>) and flag checks.</summary>
public static class EnumMetadataExtensions
{
    /// <summary>Gets the string value attribute for an enum value, or the enum name if no string value is found.</summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The string value from the StringValue attribute, or the enum name if not found.</returns>
    public static string GetStringValue(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<StringValueAttribute>();
        return attribute?.Value ?? value.ToString();
    }

    /// <summary>Determines whether the enum integral value has exactly one bit set (a single flag in a <see cref="FlagsAttribute"/>-style enum).</summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to test.</param>
    /// <returns><see langword="true"/> when the underlying value is non-zero and a power of two; otherwise <see langword="false"/>.</returns>
    public static bool IsSingleFlag<T>(this T value)
        where T : Enum
    {
        var intValue = Convert.ToInt64(value);
        return intValue != 0 && (intValue & (intValue - 1)) == 0;
    }

    /// <summary>Returns the MIME type string from <see cref="DescriptionAttribute"/> on the enum field, or the unknown MIME fallback when not present.</summary>
    /// <param name="mimeType">The <see cref="MimeType"/> value.</param>
    /// <returns>The description (MIME), or the fallback MIME string from <see cref="FileTypeInfo.Unknown"/> when no description is defined.</returns>
    public static string ToMimeString(this MimeType mimeType)
    {
        var field = mimeType.GetType().GetField(mimeType.ToString());
        var attr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
        return attr?.Description ?? FileTypeInfo.Unknown.MimeType;
    }

    /// <param name="value">The enum value.</param>
    /// <typeparam name="T">The enum type.</typeparam>
    extension<T>(T? value)
        where T : Enum
    {
        /// <summary>Gets the <see cref="DescriptionAttribute"/> text for the enum field, or the enum member name when absent.</summary>
        /// <returns>The description, or <see langword="null"/> when the receiver enum value is <see langword="null"/>.</returns>
        public string? GetDescription()
        {
            if (value is null)
                return null;

            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        /// <summary>Gets the string value attribute for an enum value, or the enum name if no string value is found.</summary>
        /// <returns>The string value from the <see cref="StringValueAttribute"/>, or the enum name if not found; <see langword="null"/> when the receiver is <see langword="null"/>.</returns>
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
