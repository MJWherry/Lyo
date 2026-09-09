namespace Lyo.Web.Components.LyoType;

/// <summary>Whether a picker value is a scalar, a <c>List&lt;T&gt;</c>, or a <c>T[]</c>.</summary>
public enum LyoTypeCollectionShape
{
    /// <summary>One value of the selected type.</summary>
    Value = 0,

    /// <summary>A <c>List&lt;T&gt;</c> of the selected type.</summary>
    List,

    /// <summary>A <c>T[]</c> of the selected type.</summary>
    Array
}
