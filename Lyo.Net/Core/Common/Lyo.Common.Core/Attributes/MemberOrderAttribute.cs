namespace Lyo.Common.Core.Attributes;

/// <summary>Places a property in the specified member order. Writers read it when their order mode is <see cref="Enums.MemberOrder.Specified" />.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MemberOrderAttribute(int order) : Attribute
{
    /// <summary>Sequence number. Lower numbers are written first.</summary>
    public int Order { get; } = order;
}