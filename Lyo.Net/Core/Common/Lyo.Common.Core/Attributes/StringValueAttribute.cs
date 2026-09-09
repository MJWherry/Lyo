using Lyo.Exceptions;

namespace Lyo.Common.Core.Attributes;

/// <summary>Wire-format string for an enum field during serialization.</summary>
/// <remarks>
/// <para>Marks an enum member with the exact text written on the wire.</para>
/// <para>Use when the serialized token must be a specific string (for example "eng" for Language.Eng).</para>
/// <para>For display text, prefer <see cref="System.ComponentModel.DescriptionAttribute" />.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field)]
public sealed class StringValueAttribute : Attribute
{
    /// <summary>Serialized string for this field.</summary>
    public string Value { get; }

    /// <summary>Stores <paramref name="value" /> as the serialization token.</summary>
    /// <param name="value">String written when this enum field is serialized.</param>
    public StringValueAttribute(string value) => Value = ArgumentHelpers.ThrowIfNullReturn(value);
}