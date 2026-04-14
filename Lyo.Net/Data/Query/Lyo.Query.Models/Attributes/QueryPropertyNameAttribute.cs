namespace Lyo.Query.Models.Attributes;

/// <summary>
/// Specifies the property path name to use when the property name differs from what is stored in saved queries. Use when EF scaffolding or DTO naming produces different
/// property names than the canonical query path.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class QueryPropertyNameAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; } = propertyName;
}