namespace Lyo.Query.Models.Attributes;

/// <summary>
/// Query-path name when the CLR property name differs from what saved queries store. Use when EF scaffolding or DTO naming diverges from the canonical query path.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class QueryPropertyNameAttribute(string propertyName) : Attribute
{
    /// <summary>Canonical query/serialization name for the attributed property.</summary>
    public string PropertyName { get; } = propertyName;
}