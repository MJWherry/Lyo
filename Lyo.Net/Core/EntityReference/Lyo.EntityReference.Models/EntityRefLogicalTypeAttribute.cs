namespace Lyo.EntityReference.Models;

/// <summary>
/// Stable logical name for <see cref="EntityRef.For{T}(object[])"/> and <see cref="EntityRef.For{T}(T, System.Func{T, object?})"/>.
/// Prefer this over CLR <see cref="Type.FullName"/> for persisted references so renames and assembly moves do not break keys.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class EntityRefLogicalTypeAttribute : Attribute
{
    /// <summary>Logical entity kind (for example <c>Order</c> or <c>Comic.Issue</c>) stored in <see cref="EntityRef.EntityType"/>.</summary>
    public string Name { get; }

    /// <summary>Initializes a new instance of the attribute.</summary>
    /// <param name="name">Non-empty stable discriminator stored in <see cref="EntityRef.EntityType"/>.</param>
    public EntityRefLogicalTypeAttribute(string name) => Name = name;
}
