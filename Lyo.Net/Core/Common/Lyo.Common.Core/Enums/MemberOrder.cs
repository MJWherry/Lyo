namespace Lyo.Common.Core.Enums;

/// <summary>How a writer orders properties or columns.</summary>
public enum MemberOrder
{
    /// <summary>Keep declaration order for a type, current key order for an object, and current column order for a table.</summary>
    Defined,

    /// <summary>Sort by the written name, ordinal.</summary>
    Alphabetical,

    /// <summary>Use <c>MemberOrderAttribute</c> and an explicit name list. Listed names come first.</summary>
    Specified
}
