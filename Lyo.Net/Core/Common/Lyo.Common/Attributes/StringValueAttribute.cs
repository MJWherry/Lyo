namespace Lyo.Common.Attributes;

/// <summary>Specifies the string value to use when serializing an enum value.</summary>
/// <remarks>
/// <para>This attribute is used to specify the string representation of an enum value for serialization purposes.</para>
/// <para>Use this attribute when the enum value needs to be serialized to a specific string value (e.g., "eng" for Language.Eng).</para>
/// <para>For human-readable descriptions, use <see cref="System.ComponentModel.DescriptionAttribute" /> instead.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field)]
public sealed class StringValueAttribute : Attribute
{
    /// <summary>Gets the string value associated with this attribute.</summary>
    public string Value { get; }

    /// <summary>Initializes a new instance of the <see cref="StringValueAttribute" /> class.</summary>
    /// <param name="value">The string value to use for serialization.</param>
    public StringValueAttribute(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
}